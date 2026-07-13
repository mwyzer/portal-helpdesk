import { useToastStore, Toast } from '@/lib/useToast';
import { X, Info, CheckCircle, AlertTriangle, AlertCircle } from 'lucide-react';
import { cn } from '@/lib/utils';

const iconMap = {
  info: Info,
  success: CheckCircle,
  warning: AlertTriangle,
  error: AlertCircle,
};

const colorMap = {
  info: 'border-info bg-info/5 text-info-foreground',
  success: 'border-success bg-success/5 text-success-foreground',
  warning: 'border-warning bg-warning/5 text-warning-foreground',
  error: 'border-destructive bg-destructive/5 text-destructive-foreground',
};

function ToastItem({ toast }: { toast: Toast }) {
  const removeToast = useToastStore((s) => s.removeToast);
  const Icon = iconMap[toast.type];

  return (
    <div
      className={cn(
        'pointer-events-auto flex items-start gap-3 rounded-lg border-l-4 p-4 shadow-lg animate-in slide-in-from-right-full',
        colorMap[toast.type],
      )}
    >
      <Icon className="h-5 w-5 mt-0.5 shrink-0" />
      <div className="flex-1 min-w-0">
        <p className="font-medium text-sm">{toast.title}</p>
        {toast.message && <p className="text-sm opacity-80 mt-0.5">{toast.message}</p>}
      </div>
      <button
        onClick={() => removeToast(toast.id)}
        className="shrink-0 opacity-60 hover:opacity-100"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  );
}

export function ToastContainer() {
  const toasts = useToastStore((s) => s.toasts);

  if (toasts.length === 0) return null;

  return (
    <div className="fixed bottom-4 right-4 z-[100] flex flex-col gap-2 max-w-sm w-full">
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} />
      ))}
    </div>
  );
}
