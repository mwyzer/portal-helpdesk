import * as React from 'react';
import { cn } from '@/lib/utils';
import { Loader2 } from 'lucide-react';

const Spinner = React.forwardRef<SVGSVGElement, React.SVGAttributes<SVGSVGElement> & { className?: string }>(
  ({ className, ...props }, ref) => {
    return <Loader2 ref={ref} className={cn('h-4 w-4 animate-spin', className)} role="status" aria-label="Loading" {...props} />;
  },
);
Spinner.displayName = 'Spinner';

export { Spinner };
