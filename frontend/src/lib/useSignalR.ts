import { useEffect, useRef, useCallback, useState } from 'react';
import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';

interface SignalRNotification {
  id: string;
  title: string;
  type: string;
  referenceId?: string;
}

type NotificationHandler = (notification: SignalRNotification) => void;

let globalConnection: HubConnection | null = null;
const handlers = new Set<NotificationHandler>();
const unreadHandlers = new Set<(count: number) => void>();

async function ensureConnection(token: string): Promise<HubConnection> {
  if (globalConnection?.state === HubConnectionState.Connected) {
    return globalConnection;
  }

  if (globalConnection?.state === HubConnectionState.Disconnected) {
    try {
      await globalConnection.start();
      return globalConnection;
    } catch {
      // will rebuild below
    }
  }

  globalConnection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => token,
    })
    .configureLogging(LogLevel.Warning)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();

  globalConnection.on('ReceiveNotification', (notification: SignalRNotification) => {
    handlers.forEach((h) => h(notification));
  });

  globalConnection.on('UnreadCountUpdated', (payload: { count: number }) => {
    unreadHandlers.forEach((h) => h(payload.count));
  });

  globalConnection.onreconnecting(() => {
    // Connection lost — silently reconnecting
  });

  globalConnection.onreconnected(() => {
    // Connection restored
  });

  await globalConnection.start();
  return globalConnection;
}

async function disconnectIfUnused(): Promise<void> {
  if (handlers.size === 0 && unreadHandlers.size === 0 && globalConnection) {
    await globalConnection.stop();
    globalConnection = null;
  }
}

export function useSignalR() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [isConnected, setIsConnected] = useState(false);
  const handlerRef = useRef<NotificationHandler | null>(null);
  const unreadHandlerRef = useRef<((count: number) => void) | null>(null);

  const onNotification = useCallback((handler: NotificationHandler) => {
    handlerRef.current = handler;
    handlers.add(handler);
  }, []);

  const onUnreadCount = useCallback((handler: (count: number) => void) => {
    unreadHandlerRef.current = handler;
    unreadHandlers.add(handler);
  }, []);

  useEffect(() => {
    if (!isAuthenticated || !accessToken) return;

    let cancelled = false;

    ensureConnection(accessToken).then((conn) => {
      if (cancelled) return;
      setIsConnected(conn.state === HubConnectionState.Connected);
    });

    return () => {
      cancelled = true;
      if (handlerRef.current) {
        handlers.delete(handlerRef.current);
        handlerRef.current = null;
      }
      if (unreadHandlerRef.current) {
        unreadHandlers.delete(unreadHandlerRef.current);
        unreadHandlerRef.current = null;
      }
      disconnectIfUnused();
    };
  }, [isAuthenticated, accessToken]);

  return { isConnected, onNotification, onUnreadCount };
}
