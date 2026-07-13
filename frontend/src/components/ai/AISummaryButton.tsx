import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Sparkles, Loader2 } from 'lucide-react';

interface AISummaryButtonProps {
  meetingId: string;
  notesCount: number;
  onSuccess?: () => void;
}

export function AISummaryButton({ meetingId, notesCount, onSuccess }: AISummaryButtonProps) {
  const queryClient = useQueryClient();
  const [showConfirm, setShowConfirm] = useState(false);

  const mutation = useMutation({
    mutationFn: () => api.post(`/meetings/${meetingId}/generate-summary`),
    onSuccess: () => {
      setShowConfirm(false);
      queryClient.invalidateQueries({ queryKey: ['meeting', meetingId] });
      onSuccess?.();
    },
  });

  const hasNotes = notesCount > 0;

  return (
    <>
      <Button
        variant="outline"
        size="sm"
        disabled={!hasNotes || mutation.isPending}
        onClick={() => setShowConfirm(true)}
        title={hasNotes ? 'Generate AI summary from meeting notes' : 'Add notes first to generate a summary'}
      >
        {mutation.isPending ? (
          <>
            <Loader2 className="mr-1.5 h-4 w-4 animate-spin" />
            Generating...
          </>
        ) : (
          <>
            <Sparkles className="mr-1.5 h-4 w-4 text-primary" />
            AI Summary
          </>
        )}
      </Button>

      {/* Confirmation dialog */}
      {showConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={() => setShowConfirm(false)}>
          <div
            className="bg-background rounded-lg shadow-lg p-6 max-w-sm mx-4"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-semibold mb-2">Generate AI Summary</h3>
            <p className="text-sm text-muted-foreground mb-4">
              This will analyze all meeting notes and create a structured summary with key decisions and action items.
              {!hasNotes && (
                <span className="block mt-1 text-warning font-medium">
                  No notes found. Add some notes first for a meaningful summary.
                </span>
              )}
            </p>
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowConfirm(false)}>
                Cancel
              </Button>
              <Button
                size="sm"
                disabled={!hasNotes || mutation.isPending}
                onClick={() => mutation.mutate()}
                className="bg-primary hover:bg-primary/90"
              >
                {mutation.isPending ? (
                  <>
                    <Loader2 className="mr-1.5 h-4 w-4 animate-spin" />
                    Generating...
                  </>
                ) : (
                  'Generate Summary'
                )}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
