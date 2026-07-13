import * as React from 'react';
import { cn } from '@/lib/utils';

interface TabsContextType {
  value: string;
  onValueChange: (value: string) => void;
}

const TabsContext = React.createContext<TabsContextType>({ value: '', onValueChange: () => {} });

interface TabsProps {
  defaultValue: string;
  value?: string;
  onValueChange?: (value: string) => void;
  className?: string;
  children: React.ReactNode;
}

const Tabs = React.forwardRef<HTMLDivElement, TabsProps>(
  ({ defaultValue, value, onValueChange, className, children }, ref) => {
    const [internalValue, setInternalValue] = React.useState(defaultValue);
    const isControlled = value !== undefined;
    const currentValue = isControlled ? value : internalValue;
    const handleValueChange = isControlled ? onValueChange! : setInternalValue;

    return (
      <TabsContext.Provider value={{ value: currentValue, onValueChange: handleValueChange }}>
        <div ref={ref} className={className}>{children}</div>
      </TabsContext.Provider>
    );
  },
);
Tabs.displayName = 'Tabs';

interface TabsListProps {
  className?: string;
  children: React.ReactNode;
}

const TabsList = React.forwardRef<HTMLDivElement, TabsListProps>(
  ({ className, children }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          'inline-flex h-10 items-center justify-center rounded-md bg-muted p-1 text-muted-foreground',
          className,
        )}
      >
        {children}
      </div>
    );
  },
);
TabsList.displayName = 'TabsList';

interface TabsTriggerProps {
  value: string;
  className?: string;
  children: React.ReactNode;
}

const TabsTrigger = React.forwardRef<HTMLButtonElement, TabsTriggerProps>(
  ({ value, className, children }, ref) => {
    const { value: selectedValue, onValueChange } = React.useContext(TabsContext);
    const isActive = selectedValue === value;

    return (
      <button
        ref={ref}
        type="button"
        className={cn(
          'inline-flex items-center justify-center whitespace-nowrap rounded-sm px-3 py-1.5 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50',
          isActive && 'bg-background text-foreground shadow-sm',
          className,
        )}
        onClick={() => onValueChange(value)}
    >
      {children}
    </button>
  );
  },
);
TabsTrigger.displayName = 'TabsTrigger';

interface TabsContentProps {
  value: string;
  className?: string;
  children: React.ReactNode;
}

const TabsContent = React.forwardRef<HTMLDivElement, TabsContentProps>(
  ({ value, className, children }, ref) => {
    const { value: selectedValue } = React.useContext(TabsContext);
    if (selectedValue !== value) return null;

    return <div ref={ref} className={className}>{children}</div>;
  },
);
TabsContent.displayName = 'TabsContent';

export { Tabs, TabsList, TabsTrigger, TabsContent };
