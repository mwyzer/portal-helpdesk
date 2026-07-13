---
name: frontend-ui-ux
description: 'UI/UX and frontend development for the AIHelpdesk React app. Use when: building or editing pages, components, forms, modals, tables, or charts; styling with Tailwind/CSS variables; adding routes or navigation; integrating APIs with React Query + Axios; managing state with Zustand; implementing dark mode; writing Playwright e2e tests; working with shadcn-style Radix UI primitives; handling SignalR real-time events; validating forms with React Hook Form + Zod. Covers React 18 + TypeScript + Vite + Tailwind + shadcn/ui patterns.'
argument-hint: '[task] — e.g., "add a new CRUD page", "create a modal form", "style a dashboard card"'
user-invocable: true
disable-model-invocation: false
---

# Frontend UI/UX Standards — AIHelpdesk

## Purpose

This document defines the frontend architecture, visual language, component patterns, accessibility rules, API conventions, and testing standards for the AIHelpdesk application.

The frontend must be: Consistent, Accessible, Responsive, Type-safe, Easy to maintain, Suitable for enterprise SaaS, and Ready for AI, RAG, and real-time features.

---

## Tech Stack

| Category        | Technology                  | Purpose                                     |
| --------------- | --------------------------- | ------------------------------------------- |
| Framework       | React 18.3                  | UI rendering                                |
| Language        | TypeScript strict mode      | Type safety                                 |
| Build Tool      | Vite 5                      | Development server and bundling             |
| Styling         | Tailwind CSS 3              | Utility-first styling                       |
| Animation       | `tailwindcss-animate`       | UI transitions and animations               |
| Design System   | CSS variables and CVA       | Theme tokens and component variants         |
| UI Primitives   | Radix UI                    | Accessible headless components              |
| Icons           | Lucide React                | Consistent icon library                     |
| Routing         | React Router v6             | Client-side routing                         |
| Server State    | TanStack React Query v5     | Fetching, caching, and mutations            |
| Client State    | Zustand v5                  | Authentication and lightweight global state |
| Forms           | React Hook Form             | Form state management                       |
| Validation      | Zod                         | Schema validation                           |
| HTTP Client     | Axios                       | API communication                           |
| Real-time       | Microsoft SignalR           | WebSocket notifications                     |
| Charts          | Recharts                    | Dashboard visualizations                    |
| Class Utilities | `clsx` and `tailwind-merge` | Conditional class merging                   |
| Testing         | Playwright                  | End-to-end browser testing                  |

---

## Visual Direction

AIHelpdesk uses a **Modern Enterprise SaaS with Calm AI** visual style.

### Visual characteristics

* Professional and trustworthy
* Clean and spacious
* Minimal visual noise
* Soft borders and subtle shadows
* Blue as the primary application color
* Violet as the AI-specific accent color
* Strong visual distinction between normal actions and AI actions
* Compact tables with comfortable dashboard spacing
* Clear loading, empty, success, warning, and error states

### Color usage

| Purpose              | Color Direction |
| -------------------- | --------------- |
| Primary action       | Blue            |
| AI feature           | Violet          |
| Success              | Emerald         |
| Warning              | Amber           |
| Error or destructive | Red             |
| Information          | Sky or blue     |
| Neutral              | Slate or gray   |

### General appearance

* Border radius: 8–12 px
* Shadows: subtle
* Cards: white or dark surface with thin border
* Forms: clear labels and visible focus states
* Gradients: use sparingly
* Red: only for errors and destructive actions
* Violet: only for AI-related features

---

## Project Structure

```
frontend/src/
├── App.tsx
├── main.tsx
├── index.css
├── components/
│   ├── ui/                    # shadcn-style primitives (button, card, dialog, input, badge, spinner, table, error-boundary)
│   ├── layout/
│   │   ├── AppLayout.tsx      # Sidebar nav + header + <Outlet/>
│   │   ├── AppSidebar.tsx
│   │   ├── AppHeader.tsx
│   │   └── ProtectedRoute.tsx # Auth guard wrapper
│   ├── ai/                    # AI chat, RAG sources, streaming, feedback, handoff
│   │   ├── AIAnswerCard.tsx
│   │   ├── AIMessageContent.tsx
│   │   ├── AISourceList.tsx
│   │   ├── AIFeedbackActions.tsx
│   │   ├── AIThinkingIndicator.tsx
│   │   └── HumanHandoffButton.tsx
│   ├── domain/                # Reusable business-specific components
│   │   ├── EmployeeTable.tsx
│   │   ├── LeaveBalanceCard.tsx
│   │   ├── ApprovalTimeline.tsx
│   │   ├── NotificationBell.tsx
│   │   └── StatusBadge.tsx
│   └── feedback/              # Loading, empty, error, skeleton states
│       ├── EmptyState.tsx
│       ├── ErrorState.tsx
│       ├── LoadingState.tsx
│       └── PageSkeleton.tsx
├── pages/                     # One file per route
├── hooks/
│   ├── useDebounce.ts
│   ├── usePagination.ts
│   └── useSignalR.ts
├── lib/
│   ├── axios.ts
│   ├── query-client.ts
│   ├── utils.ts               # cn() helper
│   ├── api-error.ts           # getApiErrorMessage()
│   └── status-variants.ts     # Centralized status color maps
├── schemas/                   # Zod validation schemas
├── services/                  # API service layer
├── store/
│   ├── authStore.ts
│   └── toastStore.ts
└── types/
    ├── api.ts                 # ApiResponse<T>, PaginatedResponse<T>
    ├── auth.ts
    ├── employee.ts
    └── ticket.ts
```

Path alias: `@/` → `./src/` (configured in both `tsconfig.json` and `vite.config.ts`).

---

## Component Placement Rules

| Location               | Use When                                               |
| ---------------------- | ------------------------------------------------------ |
| `components/ui/`       | Generic reusable components without business logic     |
| `components/layout/`   | Application shell, sidebar, header, and route guards   |
| `components/ai/`       | AI chat, RAG sources, streaming, feedback, and handoff |
| `components/domain/`   | Reusable business-specific components                  |
| `components/feedback/` | Loading, empty, error, and skeleton states             |
| `pages/`               | Route-level components                                 |
| Inline inside a page   | UI used only once on that page                         |

### Extraction rule

Extract a component when: it is used on two or more pages, contains reusable business behavior, the page becomes difficult to read, or it represents a recognizable domain concept. Do not extract components prematurely.

---

## Naming and Export Conventions

### Files

Use PascalCase for React component files: `EmployeeTable.tsx`, `AIAnswerCard.tsx`, `DashboardPage.tsx`.
Use kebab-case or camelCase for utilities: `api-error.ts`, `status-variants.ts`, `useDebounce.ts`.

### Components and Types

Named exports only — **no default exports**:

```tsx
export function EmployeeTable() { return <div />; }

interface EmployeeResponse { id: string; fullName: string; }
interface CreateEmployeeRequest { fullName: string; email: string; }
```

Do not use unclear names such as `Data`, `Item`, or `Result` unless the context is truly generic.

---

## Design Tokens (CSS Variables)

All colors must use semantic CSS variables. Do not hardcode application colors inside components unless the value comes from external data.

### `index.css` — Light theme

```css
:root {
  --background: 220 20% 98%;
  --foreground: 222 47% 11%;
  --card: 0 0% 100%;
  --card-foreground: 222 47% 11%;
  --popover: 0 0% 100%;
  --popover-foreground: 222 47% 11%;
  --primary: 221 83% 53%;
  --primary-foreground: 0 0% 100%;
  --secondary: 220 14% 96%;
  --secondary-foreground: 222 47% 11%;
  --muted: 220 14% 96%;
  --muted-foreground: 220 9% 46%;
  --accent: 220 14% 96%;
  --accent-foreground: 222 47% 11%;
  --ai: 262 83% 58%;
  --ai-foreground: 0 0% 100%;
  --success: 142 71% 45%;
  --success-foreground: 0 0% 100%;
  --warning: 38 92% 50%;
  --warning-foreground: 222 47% 11%;
  --info: 199 89% 48%;
  --info-foreground: 0 0% 100%;
  --destructive: 0 84% 60%;
  --destructive-foreground: 0 0% 100%;
  --border: 220 13% 91%;
  --input: 220 13% 91%;
  --ring: 221 83% 53%;
  --chart-1: 221 83% 53%;
  --chart-2: 142 71% 45%;
  --chart-3: 38 92% 50%;
  --chart-4: 262 83% 58%;
  --chart-5: 199 89% 48%;
  --radius: 0.625rem;
}

.dark {
  --background: 222 47% 7%;
  --foreground: 210 40% 98%;
  --card: 222 47% 9%;
  --card-foreground: 210 40% 98%;
  --primary: 217 91% 60%;
  --primary-foreground: 222 47% 11%;
  --secondary: 217 33% 17%;
  --secondary-foreground: 210 40% 98%;
  --muted: 217 33% 17%;
  --muted-foreground: 215 20% 65%;
  --accent: 217 33% 17%;
  --accent-foreground: 210 40% 98%;
  --ai: 263 70% 65%;
  --ai-foreground: 222 47% 11%;
  --border: 217 33% 17%;
  --input: 217 33% 17%;
  --ring: 224 76% 48%;
}
```

### Tailwind usage

```tsx
<div className="bg-background text-foreground" />
<Button className="bg-primary text-primary-foreground" />
<div className="border border-border bg-card text-card-foreground" />
<div className="bg-ai text-ai-foreground" />
```

**Prohibited:** `bg-[#3b82f6]`, `style={{ color: '#ef4444' }}`. **Allowed:** `bg-primary`, `text-destructive`, `border-border`.

---

## The `cn()` Utility

```tsx
import { cn } from '@/lib/utils';

<div className={cn('rounded-lg border bg-card p-4', isActive && 'border-primary', className)} />
```

Never manually concatenate Tailwind classes like `'p-4 ' + (isActive ? 'border-primary' : '')`.

---

## Component Patterns

### UI Primitive Pattern (forwardRef)

Use `React.forwardRef` for all UI primitives:

```tsx
import * as React from 'react';
import { cn } from '@/lib/utils';

const Input = React.forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input ref={ref} className={cn(
      'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2',
      'text-sm ring-offset-background placeholder:text-muted-foreground',
      'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
      'disabled:cursor-not-allowed disabled:opacity-50',
      className,
    )} {...props} />
  ),
);
Input.displayName = 'Input';  // REQUIRED
export { Input };
```

**Key rules:** Always use `React.forwardRef`, always set `.displayName`, merge classes with `cn()` (base first, then `className`), spread `...props` last, named exports only.

### Button Variants (CVA)

For components with multiple visual variants, use `class-variance-authority`:

```tsx
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva('inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50', {
  variants: {
    variant: {
      default: 'bg-primary text-primary-foreground hover:bg-primary/90',
      destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
      outline: 'border border-input bg-background hover:bg-accent hover:text-accent-foreground',
      secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
      ghost: 'hover:bg-accent hover:text-accent-foreground',
      link: 'text-primary underline-offset-4 hover:underline',
      ai: 'bg-ai text-ai-foreground hover:bg-ai/90',
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

Use the `ai` variant only for AI actions: Generate summary, Ask AI, Regenerate answer, Analyze document, Create AI recommendation.

### Radix UI Wrappers

Re-export root parts, wrap only styled parts:

```tsx
import * as DialogPrimitive from '@radix-ui/react-dialog';

const Dialog = DialogPrimitive.Root;
const DialogTrigger = DialogPrimitive.Trigger;
const DialogClose = DialogPrimitive.Close;

const DialogContent = React.forwardRef<...>(({ className, children, ...props }, ref) => (
  <DialogPortal>
    <DialogOverlay />
    <DialogPrimitive.Content ref={ref} className={cn('...', className)} {...props}>
      {children}
      <DialogPrimitive.Close className="absolute right-4 top-4 ...">
        <X className="h-4 w-4" />
        <span className="sr-only">Close</span>
      </DialogPrimitive.Close>
    </DialogPrimitive.Content>
  </DialogPortal>
));
```

### Compound Components (card, table, dialog)

```tsx
const Card = React.forwardRef<...>(({...}, ref) => <div ref={ref} ... />);
Card.displayName = 'Card';
const CardHeader = React.forwardRef<...>(({...}, ref) => <div ref={ref} ... />);
CardHeader.displayName = 'CardHeader';
// CardTitle, CardDescription, CardContent, CardFooter — same pattern
export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter };
```

---

## Status Badge Pattern

Status appearance must be centralized in `lib/status-variants.ts`:

```tsx
export type StatusVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

export const statusVariantMap: Record<string, StatusVariant> = {
  approved: 'success', completed: 'success', active: 'success',
  pending: 'warning', submitted: 'warning', waiting: 'warning',
  rejected: 'danger', failed: 'danger', cancelled: 'danger',
  processing: 'info', in_progress: 'info',
  draft: 'neutral', inactive: 'neutral',
};
```

Usage: `<StatusBadge status="approved" />`. The same status must use the same color across the entire application.

---

## Page Layout Pattern

Every page must use a consistent structure:

```tsx
export function EmployeesPage() {
  return (
    <div className="space-y-6 p-4 md:p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Employees</h1>
          <p className="text-sm text-muted-foreground">Manage employees and account access.</p>
        </div>
        <Button>Add Employee</Button>
      </div>
      <section>{/* Filters, table, cards, or page content */}</section>
    </div>
  );
}
```

**Page rules:** Every page must have a clear title, add short descriptions when useful, primary action near the title, use `space-y-6` for major sections, use `p-4 md:p-6` for responsive padding, do not place unrelated content inside one large card.

---

## API Response Contract

```tsx
export interface ApiResponse<T> {
  data: T;
  message?: string;
  traceId?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: { page: number; pageSize: number; totalItems: number; totalPages: number };
}
```

Usage:

```tsx
const response = await api.get<ApiResponse<EmployeeResponse[]>>('/employees');
return response.data.data;

// Paginated:
const response = await api.get<PaginatedResponse<EmployeeResponse>>('/employees', { params: { page, pageSize, search, status } });
return response.data;
```

---

## Service Layer

Do not write large API implementations directly inside page components.

```tsx
// services/employee.service.ts
import api from '@/lib/axios';
import type { ApiResponse, PaginatedResponse } from '@/types/api';
import type { EmployeeResponse, CreateEmployeeRequest } from '@/types/employee';

export interface EmployeeQuery { page: number; pageSize: number; search?: string; status?: string; }

export async function getEmployees(query: EmployeeQuery): Promise<PaginatedResponse<EmployeeResponse>> {
  const response = await api.get<PaginatedResponse<EmployeeResponse>>('/employees', { params: query });
  return response.data;
}

export async function createEmployee(payload: CreateEmployeeRequest): Promise<EmployeeResponse> {
  const response = await api.post<ApiResponse<EmployeeResponse>>('/employees', payload);
  return response.data.data;
}
```

---

## React Query Pattern

### Query

```tsx
const employeesQuery = useQuery({
  queryKey: ['employees', { page, pageSize, search, status }],
  queryFn: () => getEmployees({ page, pageSize, search, status }),
  placeholderData: (previousData) => previousData,
});
```

### Mutation

```tsx
const queryClient = useQueryClient();

const createMutation = useMutation({
  mutationFn: createEmployee,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['employees'] });
    addToast({ title: 'Employee created', type: 'success' });
    setDialogOpen(false);
  },
  onError: (error) => { setApiError(getApiErrorMessage(error)); },
});
```

### Query key rules

Use descriptive array keys: `['employees']`, `['employees', employeeId]`, `['tickets', { page, status, search }]`. Do not use unclear keys like `['data']`, `['list']`, `['items']`.

---

## Form Pattern (React Hook Form + Zod)

### Schema

```tsx
import { z } from 'zod';

export const employeeSchema = z.object({
  fullName: z.string().trim().min(2, 'Full name must contain at least 2 characters'),
  email: z.string().trim().email('Enter a valid email address'),
  departmentId: z.string().min(1, 'Department is required'),
});

export type EmployeeFormValues = z.infer<typeof employeeSchema>;
```

### Form component

```tsx
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

interface EmployeeFormProps {
  defaultValues?: EmployeeFormValues;
  onSubmit: (values: EmployeeFormValues) => void;
  isSubmitting?: boolean;
  apiError?: string | null;
}

export function EmployeeForm({ defaultValues, onSubmit, isSubmitting = false, apiError }: EmployeeFormProps) {
  const { register, handleSubmit, formState: { errors } } = useForm<EmployeeFormValues>({
    resolver: zodResolver(employeeSchema),
    defaultValues: defaultValues ?? { fullName: '', email: '', departmentId: '' },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {apiError && (
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {apiError}
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="fullName">Full name</Label>
        <Input id="fullName" {...register('fullName')}
          aria-invalid={Boolean(errors.fullName)}
          aria-describedby={errors.fullName ? 'fullName-error' : undefined} />
        {errors.fullName && <p id="fullName-error" className="text-sm text-destructive">{errors.fullName.message}</p>}
      </div>

      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Saving...' : 'Save'}
      </Button>
    </form>
  );
}
```

**Form conventions:** Always pair `<Label htmlFor>` with `<Input id>`, always show validation errors below inputs, disable submit during submission, change button text during submission, use `defaultValues` for edit forms, use `aria-invalid` and `aria-describedby` for accessible error linking.

---

## API Error Handling

```tsx
// lib/api-error.ts
import axios from 'axios';

interface ApiErrorResponse { message?: string; errors?: Record<string, string[]>; traceId?: string; }

export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiErrorResponse>(error)) {
    return error.response?.data?.message ?? error.message ?? 'The request could not be completed.';
  }
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred.';
}
```

---

## Loading, Error, and Empty States

Every data page must support: initial loading, background refresh, error state, empty state, success state.

```tsx
if (query.isLoading) return <PageSkeleton />;

if (query.isError) {
  return (
    <ErrorState title="Unable to load employees"
      description="Please try again or contact the administrator."
      onRetry={() => query.refetch()} />
  );
}

if (query.data?.data.length === 0) {
  return (
    <EmptyState title="No employees found"
      description="Create an employee or change the current filters."
      action={<Button onClick={() => setDialogOpen(true)}>Add Employee</Button>} />
  );
}
```

Do not display an empty table without explanation.

---

## Error Boundary

Use an Error Boundary for unexpected rendering failures (separate from React Query API errors):

```tsx
<ErrorBoundary><App /></ErrorBoundary>
```

An Error Boundary must: show a friendly message, provide a retry action, avoid exposing stack traces to users, log technical details to the monitoring system.

---

## Axios Client (`@/lib/axios`)

```tsx
import api from '@/lib/axios';

// GET (auto-prefixed with /api/)
const { data } = useQuery({ queryFn: () => api.get('/users').then(r => r.data) });

// POST / PUT / DELETE
await api.post('/users', { name: '...', email: '...' });
await api.put('/users/123', { name: '...' });
await api.delete('/users/123');

// File upload
const formData = new FormData();
formData.append('file', file);
await api.post('/employees/import', formData, { headers: { 'Content-Type': 'multipart/form-data' } });
```

**Auto-behaviors:** JWT token attached via request interceptor, 401 triggers automatic refresh token flow, failed refresh clears storage and redirects to `/login`.

---

## Table Pattern

Tables must support enterprise data workflows: server-side pagination, search, filtering, sorting, row actions, loading/empty states, responsive overflow, optional column visibility, URL-based filter state.

URL pattern: `/employees?page=2&pageSize=20&status=active&search=developer`

```tsx
<div className="overflow-x-auto rounded-lg border border-border">
  <Table>{/* Table content */}</Table>
</div>
```

For mobile, consider rendering a card list instead of forcing a wide table:

```tsx
<div className="hidden md:block"><EmployeeTable /></div>
<div className="space-y-3 md:hidden"><EmployeeCardList /></div>
```

---

## Search and Filter Pattern

Use debounced search for server requests:

```tsx
const [searchInput, setSearchInput] = useState('');
const debouncedSearch = useDebounce(searchInput, 400);
```

Do not send a request on every keystroke without debounce. Filters should be: visible, resettable, reflected in the URL, preserved after page refresh, included in the React Query key.

---

## Dashboard Pattern

### Stat grid

```tsx
<div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
  <StatCard title="Open Tickets" value={openTicketCount} icon={TicketIcon} />
  <StatCard title="Employees" value={employeeCount} icon={UsersIcon} />
</div>
```

### Chart grid

```tsx
<div className="grid gap-6 xl:grid-cols-2">
  <TicketTrendChart />
  <TicketStatusChart />
</div>
```

### Stat card

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

**Dashboard rules:** Use `useMemo` for derived chart data, wrap charts inside cards, every chart must include a title, add descriptions when metrics are unclear, add empty states when data is unavailable, use role-based query conditions through `enabled`.

---

## Chart Colors

Use CSS variables instead of hardcoded hex values:

```tsx
<Bar dataKey="value" fill="hsl(var(--chart-1))" radius={[4, 4, 0, 0]} />
```

For multiple series:

```tsx
const chartColors = ['hsl(var(--chart-1))', 'hsl(var(--chart-2))', 'hsl(var(--chart-3))', 'hsl(var(--chart-4))', 'hsl(var(--chart-5))'];
```

All Recharts charts must use `<ResponsiveContainer width="100%" height={280}>`.

### Pie Chart

```tsx
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';

<ResponsiveContainer width="100%" height={250}>
  <PieChart>
    <Pie data={data} cx="50%" cy="50%" outerRadius={80} dataKey="value"
      label={({ name, value }) => `${name}: ${value}`}>
      {data.map((_, idx) => (<Cell key={idx} fill={chartColors[idx % chartColors.length]} />))}
    </Pie>
    <Tooltip /><Legend />
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
    <Bar dataKey="value" fill="hsl(var(--chart-1))" radius={[4, 4, 0, 0]} />
  </BarChart>
</ResponsiveContainer>
```

---

## AI Component Standards

AI features must be visually distinct from normal system features. Use the `ai` color token for: AI badges, AI action buttons, thinking indicators, generated summaries, AI recommendations. Do not use AI styling for normal CRUD actions.

### AI answer structure

```tsx
<AIAnswerCard>
  <AIAnswerHeader title="AI Helpdesk Assistant" isStreaming={isStreaming} />
  <AIMessageContent>{answer}</AIMessageContent>
  <AISourceList sources={sources} />
  <AIFeedbackActions onHelpful={handleHelpful} onNotHelpful={handleNotHelpful} onRegenerate={handleRegenerate} />
  <HumanHandoffButton onClick={handleEscalation} />
</AIAnswerCard>
```

### AI answer requirements

Support: streaming state, thinking indicator, source citations, source preview, copy answer, regenerate answer, helpful/not helpful feedback, human handoff, error recovery, clear unverified-answer warning where required.

### RAG source display

Each source should show: document title, relevant excerpt, page/section when available, relevance/confidence when available, action to open the source. Never present an AI-generated answer as verified fact without showing the available source context.

---

## SignalR Integration

Use a single shared SignalR connection:

```tsx
const { onNotification, onUnreadCount, isConnected } = useSignalR();

useEffect(() => {
  return onNotification((notification) => {
    addToast({ title: notification.title, message: notification.message ?? '', type: 'info' });
  });
}, [onNotification, addToast]);
```

### SignalR requirements

* One global connection
* JWT attached through `accessTokenFactory`
* Automatic reconnect with retry delays `[0, 2000, 5000, 10000, 30000]`
* Remove handlers during cleanup
* Show connection status where operationally useful
* Avoid creating one connection per component

### Events

| Server Event | Payload | Handler |
|-------------|---------|---------|
| `ReceiveNotification` | `{ id, title, type, referenceId? }` | `onNotification(handler)` |
| `UnreadCountUpdated` | `{ count }` | `onUnreadCount(handler)` |

---

## State Management Rules

| Tool | Use For |
|------|---------|
| React Query | API data, caching, pagination, mutations, refetching, background synchronization |
| Zustand | Authentication state, toast state, sidebar state, small app-wide UI state |
| Local component state | Dialog visibility, temporary form UI, selected row, local tab selection, search input before debounce |

Do not place all state into Zustand. Do not duplicate API data from React Query inside Zustand.

---

## Routing

```tsx
// App.tsx
<Route path="employees" element={<EmployeesPage />} />

// Navigation config
{ to: '/employees', label: 'Employees', icon: Users }

// Usage
const navigate = useNavigate();
navigate('/employees');
navigate(`/employees/${employeeId}`);
```

**Route rules:** Add new routes to `App.tsx`, add visible routes to sidebar/nav config, protect restricted routes, apply role/permission checks, provide a not-found route, use lazy loading for large modules.

---

## Authorization in the UI

Frontend authorization improves UX but does not replace backend authorization:

```tsx
const canManageEmployees = user?.permissions.includes('employees.manage');
{canManageEmployees && <Button>Add Employee</Button>}
```

Use permission-based checks `hasPermission('tickets.assign')`. Avoid role-name comparisons like `user.role === 'Admin' || user.role === 'SuperAdmin'` unless the business rule explicitly depends on the role itself.

---

## Responsive Design Rules

* Page spacing: `p-4 md:p-6`
* Responsive header: `flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between`
* Responsive forms: `grid gap-4 md:grid-cols-2`
* Responsive dashboard: `grid gap-4 sm:grid-cols-2 xl:grid-cols-4`

**Mobile requirements:** Sidebar becomes a drawer, forms become one column, tables scroll horizontally or become card lists, touch targets at least 44×44 px, avoid hover-only interactions, buttons may become full width.

---

## Accessibility (a11y)

Accessibility is mandatory. Radix primitives are WCAG-compliant out of the box — do not remove their built-in ARIA attributes.

### Required additions

1. **Label-input pairing:** Every `<Input>` must have `<Label htmlFor={id}>`.
2. **Icon-only buttons:** Must have `aria-label` and `<span className="sr-only">`.
3. **Form errors:** Use `aria-invalid` and `aria-describedby` linking input → error.
4. **Loading indicators:** `<Loader2 className="animate-spin" role="status" aria-label="Loading" />`.
5. **Clickable cards:** Prefer semantic link or button. If a card cannot use one, add `role="button" tabIndex={0}` with `onKeyDown` for Enter/Space.
6. **Dialog focus:** Radix Dialog auto-traps focus — no extra code needed.
7. **Dynamic updates:** Use `aria-live` for dynamic AI and notification updates when needed.
8. Ensure sufficient contrast, keyboard navigation, visible focus styles, and do not communicate status through color alone.

---

## Dialog Standards

```tsx
<DialogContent className="sm:max-w-lg">
  <DialogHeader>
    <DialogTitle>Add Employee</DialogTitle>
    <DialogDescription>Create an employee account and assign a department.</DialogDescription>
  </DialogHeader>
  <EmployeeForm />
  <DialogFooter>
    <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
    <Button type="submit" form="employee-form">Save</Button>
  </DialogFooter>
</DialogContent>
```

Dialogs must include: title, optional description, close button, keyboard support, focus trap, clear primary/secondary actions, loading state during submission.

---

## Toast Standards

Good: `Employee created successfully.`, `Ticket assigned to Ahmad.`, `Article saved as draft.`
Avoid: `Success.`, `Done.`, `Operation completed.`

Use inline errors when the user must correct something. Do not rely only on toast messages for form validation.

---

## File Upload Pattern

```tsx
const formData = new FormData();
formData.append('file', file);
await api.post('/knowledge-documents', formData, { headers: { 'Content-Type': 'multipart/form-data' } });
```

File upload UI should show: accepted file types, maximum file size, selected file name, upload progress, validation errors, success state, option to remove/replace. For knowledge base uploads also show: indexing status, chunking status, embedding status, ready/failed state.

---

## Dark Mode

Strategy: `.dark` class on `<html>`. Use semantic tokens whenever possible (`bg-background`, `text-foreground`). Use `dark:` prefix only for one-off overrides (`bg-white dark:bg-slate-950`).

Before committing, verify: text contrast, borders, dialog surfaces, input backgrounds, charts, status badges, AI cards, hover states, focus states.

---

## Performance Rules

* Route-level lazy loading for large modules
* `useMemo` only for meaningful derived calculations
* Avoid unnecessary global state
* Debounce server search
* Server-side pagination for large datasets
* Image lazy loading
* Avoid rendering thousands of rows at once
* Keep React Query keys stable
* Do not refetch data unnecessarily
* Avoid premature optimization

---

## Playwright E2E Testing

Tests in `frontend/tests/e2e/`:

```tsx
import { test, expect } from '@playwright/test';

test('employees page loads correctly', async ({ page }) => {
  await page.goto('/employees');
  await expect(page.getByRole('heading', { name: 'Employees' })).toBeVisible();
});

test('user can create an employee', async ({ page }) => {
  await page.goto('/employees');
  await page.getByRole('button', { name: 'Add Employee' }).click();
  await page.getByLabel('Full name').fill('Ahmad Fauzan');
  await page.getByLabel('Email').fill('ahmad@example.com');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Employee created successfully.')).toBeVisible();
});
```

Run: `npm run test:e2e` or `npx playwright test`.

Prioritize testing: login, protected routes, main navigation, CRUD operations, form validation, search and filters, pagination, role restrictions, AI prompt submission, AI streaming response, source display, human handoff, real-time notifications, mobile navigation.

---

## Definition of Done

A frontend task is complete when:

* The feature works
* TypeScript passes
* Build passes
* Loading state exists
* Error state exists
* Empty state exists when relevant
* Form validation works
* API errors are user-friendly
* Responsive layout works
* Dark mode works
* Keyboard navigation works
* Permissions are respected
* Relevant tests pass
* No console errors remain

---

## Pre-Commit Checklist

### Architecture
- [ ] Components placed in correct directory
- [ ] Reusable business components extracted appropriately
- [ ] API calls use the service layer
- [ ] API data managed by React Query
- [ ] Local UI state not unnecessarily global
- [ ] Imports use `@/` alias
- [ ] Components use named exports

### TypeScript
- [ ] No unnecessary `any`
- [ ] API responses are typed
- [ ] Form values inferred from Zod
- [ ] Mutation errors use `unknown` or typed Axios errors
- [ ] `npm run build` passes

### Styling
- [ ] Semantic design tokens used
- [ ] No unnecessary hardcoded colors
- [ ] `cn()` used for conditional classes
- [ ] Dark mode verified
- [ ] Mobile layout verified
- [ ] AI features use the AI visual token
- [ ] Status colors centralized

### Forms
- [ ] React Hook Form used
- [ ] Zod validation used
- [ ] Every input has a label
- [ ] Errors use `aria-describedby`
- [ ] Submit disabled during submission
- [ ] API errors visible
- [ ] Edit forms receive default values

### Data Pages
- [ ] Loading state handled
- [ ] Error state handled
- [ ] Empty state handled
- [ ] Pagination implemented for large datasets
- [ ] Search debounced
- [ ] Filters reflected in URL
- [ ] Query keys contain all active filters

### Accessibility
- [ ] Icon-only buttons have accessible labels
- [ ] Keyboard navigation works
- [ ] Focus states visible
- [ ] Dialog focus managed correctly
- [ ] Status not communicated only through color
- [ ] Dynamic updates use appropriate ARIA behavior

### AI Features
- [ ] Streaming state visible
- [ ] Sources displayed
- [ ] AI errors can be retried
- [ ] Feedback controls available
- [ ] Human handoff available where required
- [ ] Unverified answers clearly identified

### Testing
- [ ] Main user flow has a Playwright test
- [ ] Validation behavior tested
- [ ] Permission behavior tested
- [ ] No browser console errors
- [ ] Existing tests still pass

---

## Final Principles

1. Prefer consistency over personal preference.
2. Prefer semantic design tokens over hardcoded styles.
3. Prefer accessible primitives over custom interactive elements.
4. Prefer server state in React Query.
5. Prefer local state unless multiple areas truly need it.
6. Prefer permission checks over hardcoded role comparisons.
7. Prefer reusable domain components over duplicated business UI.
8. Prefer clear loading, error, and empty states.
9. Treat AI-generated content differently from verified system data.
10. Build every feature for desktop, mobile, keyboard, and dark mode.
