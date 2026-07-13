---
name: frontend-ui-ux
description: 'UI/UX and frontend development for the AIHelpdesk React app. Use when: building or editing pages, components, forms, modals, tables, or charts; styling with Tailwind/CSS variables; adding routes or navigation; integrating APIs with React Query + Axios; managing state with Zustand; implementing dark mode; writing Playwright e2e tests; working with shadcn-style Radix UI primitives; handling SignalR real-time events; validating forms with React Hook Form + Zod. Covers React 18 + TypeScript + Vite + Tailwind + shadcn/ui patterns.'
argument-hint: '[task] — e.g., "add a new CRUD page", "create a modal form", "style a dashboard card"'
user-invocable: true
disable-model-invocation: false
---

# Frontend UI/UX — AIHelpdesk

## Tech Stack

| Category | Library | Purpose |
|----------|---------|---------|
| Framework | React 18.3 + TypeScript strict | UI layer |
| Build | Vite 5 | Dev server + bundler |
| Styling | Tailwind CSS 3 + `tailwindcss-animate` | Utility-first CSS with animations |
| Design System | CSS variables (HSL) + `class-variance-authority` | shadcn/ui-style theming + variant props |
| UI Primitives | Radix UI (Avatar, Dialog, DropdownMenu, Label, Select, Separator, Slot, Switch, Tabs, Toast, Tooltip) | Accessible headless components |
| Icons | Lucide React | Icon library |
| Routing | React Router v6 | Client-side routing |
| Server State | TanStack React Query v5 | Data fetching, caching, mutations |
| Client State | Zustand v5 | Lightweight stores (`authStore`, `toastStore`) |
| Forms | React Hook Form + Zod + `@hookform/resolvers` | Form state + schema validation |
| HTTP | Axios | API client with JWT interceptor |
| Real-time | `@microsoft/signalr` | WebSocket notifications |
| Charts | Recharts | Bar, pie, line charts |
| Class utils | `clsx` + `tailwind-merge` | `cn()` helper |
| Testing | Playwright | End-to-end browser tests |

## Project Structure

```
frontend/src/
├── App.tsx                  # Route definitions (public + protected)
├── main.tsx                 # Entry point, ReactDOM.render
├── index.css                # Tailwind directives + CSS variables (light/dark)
├── components/
│   ├── ui/                  # shadcn-style primitives (button, card, input, table, dialog, etc.)
│   ├── layout/
│   │   ├── AppLayout.tsx    # Sidebar nav + header + <Outlet/>
│   │   └── ProtectedRoute.tsx  # Auth guard wrapper
│   ├── AISummaryButton.tsx  # Feature components
│   ├── ApprovalTimeline.tsx
│   ├── EmployeeTable.tsx
│   ├── LeaveBalanceCard.tsx
│   ├── NotificationBell.tsx
│   ├── ParticipantSelector.tsx
│   └── ToastContainer.tsx
├── pages/                   # One file per route (e.g., DashboardPage.tsx)
├── lib/
│   ├── utils.ts             # cn() class merge helper
│   ├── axios.ts             # API client with auth interceptors
│   ├── useSignalR.ts        # SignalR hook
│   └── useToast.ts          # Toast Zustand store
└── store/
    └── authStore.ts         # Auth state (user, login, logout)
```

Path alias: `@/` → `./src/` (configured in both `tsconfig.json` and `vite.config.ts`).

## Component Placement Rules

Use this decision tree to determine where a new component belongs:

| Location | When to Use | Example |
|----------|-------------|---------|
| `components/ui/` | Generic, reusable primitive without business logic; could live in any shadcn/ui project | `button.tsx`, `card.tsx`, `dialog.tsx`, `input.tsx`, `table.tsx`, `spinner.tsx`, `badge.tsx` |
| `components/` (root) | Feature-specific, reusable across multiple pages; contains domain concepts | `EmployeeTable.tsx`, `LeaveBalanceCard.tsx`, `NotificationBell.tsx`, `ParticipantSelector.tsx` |
| `components/layout/` | App shell, navigation, auth guards | `AppLayout.tsx`, `ProtectedRoute.tsx` |
| Inline in `pages/` | One-off UI that is only used on that single page; do NOT extract premature abstractions | A dashboard-specific stat card, a page-specific form section |

**Rule of thumb:** If a component is used on 2+ pages, extract it to `components/`. If it has zero business logic and is purely presentational, it goes in `components/ui/`.

## Component Patterns

### 1. UI Primitives (shadcn-style)

All UI components in `components/ui/` follow these conventions:

```tsx
import * as React from 'react';
import { cn } from '@/lib/utils';

// Always use React.forwardRef
const Component = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('base-classes', className)} {...props} />
  ),
);
Component.displayName = 'Component';  // REQUIRED for forwardRef

export { Component };  // Named export
```

**Key rules:**
- Use `React.forwardRef` for all primitives
- Set `.displayName` after every `forwardRef`
- Merge classes with `cn()` — always put base classes first, then `className`
- Spread `...props` last so consumers can override
- Named exports only (no default exports)

### 2. Radix UI Wrappers (dialog, select, dropdown-menu)

When wrapping Radix primitives, re-export the root parts and wrap content parts:

```tsx
import * as DialogPrimitive from '@radix-ui/react-dialog';

// Re-export unmodified primitives
const Dialog = DialogPrimitive.Root;
const DialogTrigger = DialogPrimitive.Trigger;
const DialogClose = DialogPrimitive.Close;

// Wrap only the styled parts with forwardRef
const DialogContent = React.forwardRef<...>(({ className, children, ...props }, ref) => (
  <DialogPortal>
    <DialogOverlay />
    <DialogPrimitive.Content ref={ref} className={cn('...', className)} {...props}>
      {children}
      <DialogPrimitive.Close className="absolute right-4 top-4 ...">
        <X className="h-4 w-4" />
        <span className="sr-only">Close</span>  {/* Always include sr-only for a11y */}
      </DialogPrimitive.Close>
    </DialogPrimitive.Content>
  </DialogPortal>
));
```

### 3. CVA Variants (button)

For components with multiple style variants, use `class-variance-authority`:

```tsx
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva('base-classes', {
  variants: {
    variant: {
      default: 'bg-primary text-primary-foreground hover:bg-primary/90',
      destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
      outline: 'border border-input bg-background hover:bg-accent',
      secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
      ghost: 'hover:bg-accent hover:text-accent-foreground',
      link: 'text-primary underline-offset-4 hover:underline',
    },
    size: {
      default: 'h-10 px-4 py-2',
      sm: 'h-9 rounded-md px-3',
      lg: 'h-11 rounded-md px-8',
      icon: 'h-10 w-10',
    },
  },
  defaultVariants: { variant: 'default', size: 'default' },
});
```

### 4. Simple Variants (badge)

For fewer variants, a plain `Record<string, string>` is cleaner than CVA:

```tsx
const variantStyles: Record<string, string> = {
  default: 'bg-primary text-primary-foreground hover:bg-primary/80',
  secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
  destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/80',
  outline: 'text-foreground border border-input',
  success: 'bg-emerald-500 text-white hover:bg-emerald-600',
  warning: 'bg-amber-500 text-white hover:bg-amber-600',
};

export function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  return <span className={cn('base-classes', variantStyles[variant], className)} {...props} />;
}
```

### 5. Compound Components (card, table, dialog)

Compose multiple sub-components with `displayName`:

```tsx
const Card = React.forwardRef<...>(({...}, ref) => <div ref={ref} ... />);
Card.displayName = 'Card';
const CardHeader = React.forwardRef<...>(({...}, ref) => <div ref={ref} ... />);
CardHeader.displayName = 'CardHeader';
// CardTitle, CardDescription, CardContent, CardFooter — same pattern

export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter };
```

## Page Patterns

### Data Fetching Page (CRUD)

Every data page follows this structure:

```tsx
// ── Types ──────────────────────────────────────────
interface ThingResponse {
  id: string;
  name: string;
  // ...
}

// ── Component ──────────────────────────────────────
export function ThingsPage() {
  const navigate = useNavigate();

  // 1. Query (READ)
  const { data, isLoading, error } = useQuery<ThingResponse[]>({
    queryKey: ['things'],
    queryFn: () => api.get('/things').then((r) => r.data),
  });

  // 2. Mutation (CREATE/UPDATE/DELETE)
  const queryClient = useQueryClient();
  const createMutation = useMutation({
    mutationFn: (body: CreateThingDto) => api.post('/things', body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['things'] }),
  });

  // 3. Render
  if (isLoading) return <Spinner />;
  if (error) return <ErrorState />;

  return (
    <div className="space-y-6 p-6">
      {/* Header + action button */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Things</h1>
        <Button onClick={() => setModalOpen(true)}>
          <Plus className="mr-2 h-4 w-4" /> Add Thing
        </Button>
      </div>
      {/* Content: table, cards, or list */}
      {/* Modal for create/edit */}
    </div>
  );
}
```

### Form Pattern (React Hook Form + Zod)

```tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const schema = z.object({
  name: z.string().min(1, 'Name is required'),
  email: z.string().email('Invalid email'),
  departmentId: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

function MyForm({ defaultValues, onSubmit }: {
  defaultValues?: FormData;
  onSubmit: (data: FormData) => void;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: defaultValues || { name: '', email: '' },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <Label htmlFor="name">Name</Label>
        <Input id="name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
      </div>
      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Saving...' : 'Save'}
      </Button>
    </form>
  );
}
```

**Form conventions:**
- Always pair `<Label htmlFor="...">` with `<Input id="...">` for a11y
- Always show validation errors below the input: `{errors.field && <p className="text-sm text-destructive">{errors.field.message}</p>}`
- Disable submit button during submission: `disabled={isSubmitting}`
- Change button text during submission: `{isSubmitting ? 'Saving...' : 'Save'}`
- Use `defaultValues` prop for edit forms; pass `null` defaults for create forms

## Styling System

### Design Tokens (CSS Variables)

All colors are HSL-based CSS variables defined in `index.css`:

```css
:root {
  --background: 0 0% 100%;
  --foreground: 240 10% 3.9%;
  --primary: 221 83% 53%;
  --primary-foreground: 0 0% 100%;
  --muted: 240 4.8% 95.9%;
  --muted-foreground: 240 3.8% 46.1%;
  --border: 240 5.9% 90%;
  --ring: 221 83% 53%;
  --radius: 0.5rem;
  /* ... */
}
.dark {
  --background: 240 10% 3.9%;
  --foreground: 0 0% 98%;
  /* ... */
}
```

**Usage in Tailwind:** Reference via `bg-background`, `text-foreground`, `border-border`, etc.

### Dark Mode

- Strategy: `class` (`.dark` class on `<html>`)
- All components support dark mode via CSS variable mappers
- Tailwind config extends colors to map to CSS variables
- Use `dark:` prefix for one-off overrides: `dark:bg-gray-900`

### The cn() Helper

```tsx
import { cn } from '@/lib/utils';
// Merges Tailwind classes, resolving conflicts via tailwind-merge
<div className={cn('text-sm', isActive && 'text-primary', className)} />
```

### Status Color Maps

For status badges across the app, use inline record maps (not global constants):

```tsx
const statusColor = (status: string) => {
  const map: Record<string, string> = {
    Approved: 'bg-green-100 text-green-800',
    Rejected: 'bg-red-100 text-red-800',
    Submitted: 'bg-blue-100 text-blue-800',
    Draft: 'bg-gray-100 text-gray-800',
  };
  return map[status] || 'bg-gray-100 text-gray-800';
};

// Usage:
<Badge className={statusColor(item.status)}>{item.status}</Badge>
```

## Charts & Dashboard Patterns

Recharts is used on the dashboard. All chart components must be wrapped in `<ResponsiveContainer>`.

### Pie Chart

```tsx
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';

const PIE_COLORS = ['#22c55e', '#ef4444', '#f59e0b', '#8b5cf6', '#3b82f6', '#ec4899'];

<ResponsiveContainer width="100%" height={250}>
  <PieChart>
    <Pie data={data} cx="50%" cy="50%" outerRadius={80} dataKey="value"
      label={({ name, value }) => `${name}: ${value}`}>
      {data.map((_, idx) => (
        <Cell key={idx} fill={PIE_COLORS[idx % PIE_COLORS.length]} />
      ))}
    </Pie>
    <Tooltip />
    <Legend />
  </PieChart>
</ResponsiveContainer>
```

### Bar Chart

```tsx
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

<ResponsiveContainer width="100%" height={250}>
  <BarChart data={data}>
    <CartesianGrid strokeDasharray="3 3" />
    <XAxis dataKey="name" tick={{ fontSize: 11 }} />
    <YAxis allowDecimals={false} />
    <Tooltip />
    <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
  </BarChart>
</ResponsiveContainer>
```

### Stat Cards (Dashboard)

```tsx
<Card>
  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
    <CardTitle className="text-sm font-medium">Total Employees</CardTitle>
    <Users className="h-4 w-4 text-blue-600" />
  </CardHeader>
  <CardContent>
    <div className="text-2xl font-bold">{count ?? '—'}</div>
  </CardContent>
</Card>
```

**Dashboard conventions:**
- Use `useMemo` for derived data (chart transformations, filtered lists)
- Wrap charts in `<Card>` + `<CardHeader>`/`<CardContent>`
- Use `enabled:` query option for role-conditional data fetching (e.g., `enabled: isAdmin`)
- Stat card grid: `grid gap-4 md:grid-cols-2 lg:grid-cols-4`
- Chart grid: `grid gap-6 lg:grid-cols-2`

## Error Handling Patterns

### Data Fetching Errors (React Query)

```tsx
const { data, isLoading, error } = useQuery<Response>({
  queryKey: ['things'],
  queryFn: () => api.get('/things').then((r) => r.data),
});

if (isLoading) return <Spinner />;
if (error) {
  return (
    <Card className="m-6">
      <CardContent className="pt-6 text-center text-destructive">
        Failed to load data. Please try again.
      </CardContent>
    </Card>
  );
}
```

### Mutation Errors (Inline)

```tsx
const [apiError, setApiError] = useState<string | null>(null);

const createMutation = useMutation({
  mutationFn: (body) => api.post('/things', body),
  onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['things'] }); setShowCreate(false); },
  onError: (err: any) => {
    setApiError(err.response?.data?.message || 'An error occurred');
  },
});

// In JSX:
{apiError && <p className="text-sm text-destructive">{apiError}</p>}
```

### Error Display Styles

| Context | Pattern |
|---------|---------|
| Form field validation | `<p className="text-sm text-destructive">{errors.field.message}</p>` |
| API error above form | `<p className="text-sm text-destructive mb-4">{apiError}</p>` |
| Page-level error state | `<Card><CardContent className="pt-6 text-center text-destructive">...</CardContent></Card>` |
| Empty state | `<Card><CardContent className="pt-6 text-center text-muted-foreground">No data found.</CardContent></Card>` |

**No ErrorBoundary class component is used in this project** — errors are handled inline via React Query's error state and mutation `onError` callbacks. Do not introduce `componentDidCatch`-style error boundaries.

## State Management

### Zustand Stores

```tsx
import { create } from 'zustand';

interface MyStore {
  value: string;
  setValue: (v: string) => void;
}

export const useMyStore = create<MyStore>((set) => ({
  value: '',
  setValue: (v) => set({ value: v }),
}));
```

**Existing stores:**
- `useAuthStore` (`@/store/authStore`): `user`, `login()`, `logout()`, `isAuthenticated`
- `useToastStore` (`@/lib/useToast`): `toasts`, `addToast()`, `removeToast()`

### React Query

- Always use `queryKey` arrays with descriptive names
- Use `useMutation` with `onSuccess: () => queryClient.invalidateQueries(...)` for mutations
- Handle loading state with `<Spinner />` and errors with inline error messages
- Use `enabled:` for conditional queries (role-gated, dependent on other data)

## SignalR Real-Time Integration

The `useSignalR` hook provides a singleton WebSocket connection to `/hubs/notifications`.

### Hook API

```tsx
import { useSignalR } from '@/lib/useSignalR';

const { onNotification, onUnreadCount, isConnected } = useSignalR();

// Register handlers in useEffect
useEffect(() => {
  onNotification((notification) => {
    addToast({ title: notification.title, message: '', type: 'info' });
  });
}, [onNotification, addToast]);

useEffect(() => {
  onUnreadCount((count) => {
    setUnreadCount(count);
  });
}, [onUnreadCount]);
```

### Architecture

- **Global singleton:** One `HubConnection` shared across all components via module-level `globalConnection` variable
- **Handler pattern:** `Set<Function>` collections; `onNotification` / `onUnreadCount` add/remove handlers
- **Reconnect:** Automatic retry with backoff `[0, 2000, 5000, 10000, 30000]` ms
- **Lifecycle:** Connection starts on first subscriber, stops when last subscriber unregisters
- **Auth:** JWT token from `useAuthStore` attached via `accessTokenFactory`

### Events

| Server Event | Payload | Handler |
|-------------|---------|---------|
| `ReceiveNotification` | `{ id, title, type, referenceId? }` | `onNotification(handler)` |
| `UnreadCountUpdated` | `{ count }` | `onUnreadCount(handler)` |

## Accessibility (a11y)

### Radix UI Provides Base a11y

All Radix primitives (Dialog, Select, DropdownMenu, Tabs, Switch, Tooltip) are WCAG-compliant out of the box. Do not remove Radix's built-in ARIA attributes.

### Required Additions in This Project

1. **Screen-reader-only labels:** Always add `<span className="sr-only">` for icon-only buttons and close buttons:
   ```tsx
   <DialogPrimitive.Close>
     <X className="h-4 w-4" />
     <span className="sr-only">Close</span>
   </DialogPrimitive.Close>
   ```

2. **Label-input pairing:** Every `<Input>` must have a corresponding `<Label htmlFor={id}>`:
   ```tsx
   <Label htmlFor="email">Email</Label>
   <Input id="email" {...register('email')} />
   ```

3. **Focus management for modals:** Radix Dialog auto-traps focus. After opening a modal, focus automatically moves to the first focusable element. No extra code needed.

4. **Keyboard navigation for clickable cards:** Ensure navigational cards are keyboard-accessible:
   ```tsx
   <Card
     className="cursor-pointer hover:border-primary/50 transition-colors"
     tabIndex={0}
     role="button"
     onClick={() => navigate('/target')}
     onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') navigate('/target'); }}
   >
   ```

5. **Loading indicators:** `<Spinner />` renders a `<Loader2>` icon with `animate-spin`. Screen readers need `role="status"` and `aria-label`:
   ```tsx
   // In Spinner component:
   <Loader2 className="animate-spin" role="status" aria-label="Loading" />
   ```

6. **Form error association:** Use `aria-describedby` to link inputs to error messages:
   ```tsx
   <Input id="email" {...register('email')} aria-describedby="email-error" />
   {errors.email && <p id="email-error" className="text-sm text-destructive">{errors.email.message}</p>}
   ```

## API Integration

### Axios Client (`@/lib/axios`)

```tsx
import api from '@/lib/axios';

// GET (auto-prefixed with /api/)
const { data } = useQuery({ queryFn: () => api.get('/users').then(r => r.data) });

// POST
await api.post('/users', { name: '...', email: '...' });

// PUT
await api.put('/users/123', { name: '...' });

// DELETE
await api.delete('/users/123');

// File upload
const formData = new FormData();
formData.append('file', file);
await api.post('/employees/import', formData, {
  headers: { 'Content-Type': 'multipart/form-data' },
});
```

**Auto-behaviors:**
- JWT token attached automatically via request interceptor
- 401 responses trigger automatic refresh token flow
- Failed refresh clears storage and redirects to `/login`

## Routing

```tsx
// Adding a new route in App.tsx:
<Route path="my-new-page" element={<MyNewPage />} />

// Adding a nav item in AppLayout.tsx:
{ to: '/my-new-page', label: 'My Page', icon: PackageIcon },

// Navigation in components:
const navigate = useNavigate();
navigate('/my-new-page');
navigate(`/things/${id}`);  // dynamic params
```

## Testing (Playwright E2E)

```ts
// frontend/tests/e2e/all-phases.spec.ts
import { test, expect } from '@playwright/test';

test('page loads correctly', async ({ page }) => {
  await page.goto('/my-page');
  await expect(page.locator('h1')).toContainText('My Page');
});
```

Run: `npm run test:e2e` (or `npx playwright test`)

## Checklist: Before Committing Frontend Changes

- [ ] All new components have `displayName` set
- [ ] `cn()` used for conditional/Tailwind class merging (never manual string concat)
- [ ] Forms use `react-hook-form` + `zod` schema validation
- [ ] Every `<Input>` has a matching `<Label htmlFor={id}>` (a11y)
- [ ] Form errors use `aria-describedby` linking input → error message (a11y)
- [ ] Icon-only buttons have `<span className="sr-only">` (a11y)
- [ ] Clickable cards have `tabIndex={0}`, `role="button"`, and `onKeyDown` (a11y)
- [ ] API calls use the shared `api` axios instance (not raw axios/fetch)
- [ ] React Query `queryKey` arrays are descriptive (e.g., `['users', userId]`)
- [ ] Loading states handled with `<Spinner />` component
- [ ] Error states display user-friendly messages
- [ ] Dark mode works (check `.dark` class renders correctly)
- [ ] Mobile sidebar toggle works (responsive layout)
- [ ] New routes added to both `App.tsx` and `AppLayout.tsx` nav
- [ ] New components placed in correct directory (ui/ vs components/ vs inline)
- [ ] No hardcoded colors — use Tailwind theme classes or CSS variable tokens
- [ ] Imports use `@/` alias (not relative `../../` paths)
- [ ] Named exports for all components (no `export default`)
- [ ] TypeScript compiles: `npm run build` passes
