---

name: frontend-ui-ux
description: >
Frontend architecture, UI/UX standards, and development conventions for the
AIHelpdesk React application. Use when creating or editing pages, components,
forms, dialogs, tables, charts, navigation, API integrations, real-time
notifications, dark mode, responsive layouts, or Playwright tests.
argument-hint: >
[task] — for example: "create an employee CRUD page",
"add an AI answer card", or "build a responsive dashboard"
user-invocable: true
disable-model-invocation: false
-------------------------------

# Frontend UI/UX Standards — AIHelpdesk

## 1. Purpose

This document defines the frontend architecture, visual language, component patterns, accessibility rules, API conventions, and testing standards for the AIHelpdesk application.

The frontend must be:

* Consistent
* Accessible
* Responsive
* Type-safe
* Easy to maintain
* Suitable for enterprise SaaS
* Ready for AI, RAG, and real-time features

---

## 2. Technology Stack

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

## 3. Visual Direction

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

## 4. Project Structure

```text
frontend/src/
├── App.tsx
├── main.tsx
├── index.css
│
├── components/
│   ├── ui/
│   │   ├── button.tsx
│   │   ├── card.tsx
│   │   ├── dialog.tsx
│   │   ├── input.tsx
│   │   ├── badge.tsx
│   │   ├── spinner.tsx
│   │   ├── table.tsx
│   │   └── error-boundary.tsx
│   │
│   ├── layout/
│   │   ├── AppLayout.tsx
│   │   ├── AppSidebar.tsx
│   │   ├── AppHeader.tsx
│   │   └── ProtectedRoute.tsx
│   │
│   ├── ai/
│   │   ├── AIAnswerCard.tsx
│   │   ├── AIMessageContent.tsx
│   │   ├── AISourceList.tsx
│   │   ├── AIFeedbackActions.tsx
│   │   ├── AIThinkingIndicator.tsx
│   │   └── HumanHandoffButton.tsx
│   │
│   ├── domain/
│   │   ├── EmployeeTable.tsx
│   │   ├── LeaveBalanceCard.tsx
│   │   ├── ApprovalTimeline.tsx
│   │   ├── NotificationBell.tsx
│   │   └── StatusBadge.tsx
│   │
│   └── feedback/
│       ├── EmptyState.tsx
│       ├── ErrorState.tsx
│       ├── LoadingState.tsx
│       └── PageSkeleton.tsx
│
├── pages/
│   ├── DashboardPage.tsx
│   ├── EmployeesPage.tsx
│   ├── TicketsPage.tsx
│   └── KnowledgeBasePage.tsx
│
├── hooks/
│   ├── useDebounce.ts
│   ├── usePagination.ts
│   └── useSignalR.ts
│
├── lib/
│   ├── axios.ts
│   ├── query-client.ts
│   ├── utils.ts
│   ├── api-error.ts
│   └── status-variants.ts
│
├── schemas/
│   ├── employee.schema.ts
│   ├── ticket.schema.ts
│   └── auth.schema.ts
│
├── services/
│   ├── employee.service.ts
│   ├── ticket.service.ts
│   └── knowledge-base.service.ts
│
├── store/
│   ├── authStore.ts
│   └── toastStore.ts
│
└── types/
    ├── api.ts
    ├── auth.ts
    ├── employee.ts
    └── ticket.ts
```

Use the following alias:

```text
@/ → ./src/
```

Configure the alias in both:

* `tsconfig.json`
* `vite.config.ts`

---

## 5. Component Placement Rules

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

Extract a component when:

* It is used on two or more pages
* It contains reusable business behavior
* The page becomes difficult to read
* It represents a recognizable domain concept

Do not extract components prematurely.

---

## 6. Naming and Export Conventions

### Files

Use PascalCase for React component files:

```text
EmployeeTable.tsx
AIAnswerCard.tsx
DashboardPage.tsx
```

Use kebab-case or camelCase for utilities:

```text
api-error.ts
status-variants.ts
useDebounce.ts
```

### Components

Use named exports:

```tsx
export function EmployeeTable() {
  return <div />;
}
```

Avoid default exports:

```tsx
// Avoid
export default EmployeeTable;
```

### Types

Use descriptive names:

```tsx
interface EmployeeResponse {
  id: string;
  fullName: string;
}

interface CreateEmployeeRequest {
  fullName: string;
  email: string;
}
```

Do not use unclear names such as:

```tsx
interface Data {}
interface Item {}
interface Result {}
```

unless the context is truly generic.

---

## 7. Design Tokens

All colors must use semantic CSS variables.

Do not hardcode application colors inside components unless the value comes from external data.

### `index.css`

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

  --popover: 222 47% 9%;
  --popover-foreground: 210 40% 98%;

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

### Prohibited

```tsx
<div className="bg-[#3b82f6]" />
<div style={{ color: '#ef4444' }} />
```

### Allowed

```tsx
<div className="bg-primary" />
<div className="text-destructive" />
<div className="border-border" />
```

---

## 8. The `cn()` Utility

Use `cn()` for all conditional or merged classes.

```tsx
import { cn } from '@/lib/utils';

<div
  className={cn(
    'rounded-lg border bg-card p-4',
    isActive && 'border-primary',
    className,
  )}
/>
```

Do not manually concatenate Tailwind classes:

```tsx
// Avoid
<div className={'p-4 ' + (isActive ? 'border-primary' : '')} />
```

---

## 9. UI Primitive Pattern

Use `React.forwardRef` when the consumer may need access to the underlying DOM element.

Typical examples:

* Button
* Input
* Textarea
* Dialog content
* Select trigger
* Table elements
* Components integrated with Radix UI
* Components used for focus management

```tsx
import * as React from 'react';
import { cn } from '@/lib/utils';

const Input = React.forwardRef<
  HTMLInputElement,
  React.InputHTMLAttributes<HTMLInputElement>
>(({ className, ...props }, ref) => {
  return (
    <input
      ref={ref}
      className={cn(
        'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2',
        'text-sm ring-offset-background',
        'placeholder:text-muted-foreground',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
        'disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      {...props}
    />
  );
});

Input.displayName = 'Input';

export { Input };
```

Do not force `forwardRef` on components that do not expose or require a DOM reference.

---

## 10. Button Variants

Use `class-variance-authority` for components with multiple visual variants.

```tsx
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'inline-flex items-center justify-center rounded-md text-sm font-medium',
  {
    variants: {
      variant: {
        default:
          'bg-primary text-primary-foreground hover:bg-primary/90',
        destructive:
          'bg-destructive text-destructive-foreground hover:bg-destructive/90',
        outline:
          'border border-input bg-background hover:bg-accent',
        secondary:
          'bg-secondary text-secondary-foreground hover:bg-secondary/80',
        ghost:
          'hover:bg-accent hover:text-accent-foreground',
        link:
          'text-primary underline-offset-4 hover:underline',
        ai:
          'bg-ai text-ai-foreground hover:bg-ai/90',
      },
      size: {
        default: 'h-10 px-4 py-2',
        sm: 'h-9 rounded-md px-3',
        lg: 'h-11 rounded-md px-8',
        icon: 'h-10 w-10',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}
```

Use the AI variant only for actions such as:

* Generate summary
* Ask AI
* Regenerate answer
* Analyze document
* Create AI recommendation

---

## 11. Status Badge Pattern

Status appearance must be centralized.

Do not define different color maps independently on every page.

### `lib/status-variants.ts`

```tsx
export type StatusVariant =
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'neutral';

export const statusVariantMap: Record<string, StatusVariant> = {
  approved: 'success',
  completed: 'success',
  active: 'success',

  pending: 'warning',
  submitted: 'warning',
  waiting: 'warning',

  rejected: 'danger',
  failed: 'danger',
  cancelled: 'danger',

  processing: 'info',
  in_progress: 'info',

  draft: 'neutral',
  inactive: 'neutral',
};
```

### Usage

```tsx
<StatusBadge status="approved" />
```

The same status must use the same color across the entire application.

---

## 12. Page Layout Pattern

Every page should use a consistent structure.

```tsx
export function EmployeesPage() {
  return (
    <div className="space-y-6 p-4 md:p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            Employees
          </h1>
          <p className="text-sm text-muted-foreground">
            Manage employees and account access.
          </p>
        </div>

        <Button>
          Add Employee
        </Button>
      </div>

      <section>
        {/* Filters, table, cards, or page content */}
      </section>
    </div>
  );
}
```

### Page rules

* Every page must have a clear title
* Add a short description when useful
* Primary action should appear near the title
* Use `space-y-6` for major page sections
* Use `p-4 md:p-6` for responsive page padding
* Do not place unrelated content inside one large card

---

## 13. API Response Contract

Use a consistent API response structure.

```tsx
export interface ApiResponse<T> {
  data: T;
  message?: string;
  traceId?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  };
}
```

Example:

```tsx
const response = await api.get<ApiResponse<EmployeeResponse[]>>(
  '/employees',
);

return response.data.data;
```

For paginated endpoints:

```tsx
const response = await api.get<PaginatedResponse<EmployeeResponse>>(
  '/employees',
  {
    params: {
      page,
      pageSize,
      search,
      status,
    },
  },
);

return response.data;
```

---

## 14. Service Layer

Do not write large API implementations directly inside page components.

### `services/employee.service.ts`

```tsx
import api from '@/lib/axios';
import type {
  ApiResponse,
  PaginatedResponse,
} from '@/types/api';
import type {
  EmployeeResponse,
  CreateEmployeeRequest,
} from '@/types/employee';

export interface EmployeeQuery {
  page: number;
  pageSize: number;
  search?: string;
  status?: string;
}

export async function getEmployees(
  query: EmployeeQuery,
): Promise<PaginatedResponse<EmployeeResponse>> {
  const response = await api.get<
    PaginatedResponse<EmployeeResponse>
  >('/employees', {
    params: query,
  });

  return response.data;
}

export async function createEmployee(
  payload: CreateEmployeeRequest,
): Promise<EmployeeResponse> {
  const response = await api.post<
    ApiResponse<EmployeeResponse>
  >('/employees', payload);

  return response.data.data;
}
```

---

## 15. React Query Pattern

### Query

```tsx
const employeesQuery = useQuery({
  queryKey: [
    'employees',
    {
      page,
      pageSize,
      search,
      status,
    },
  ],
  queryFn: () =>
    getEmployees({
      page,
      pageSize,
      search,
      status,
    }),
  placeholderData: (previousData) => previousData,
});
```

### Mutation

```tsx
const queryClient = useQueryClient();

const createMutation = useMutation({
  mutationFn: createEmployee,
  onSuccess: () => {
    queryClient.invalidateQueries({
      queryKey: ['employees'],
    });

    addToast({
      title: 'Employee created',
      type: 'success',
    });

    setDialogOpen(false);
  },
  onError: (error) => {
    setApiError(getApiErrorMessage(error));
  },
});
```

### Query key rules

Use descriptive array keys:

```tsx
['employees']
['employees', employeeId]
['tickets', { page, status, search }]
['knowledge-articles', articleId]
```

Do not use unclear keys:

```tsx
['data']
['list']
['items']
```

---

## 16. Form Pattern

Use:

* React Hook Form
* Zod
* `zodResolver`
* Accessible labels
* Inline validation errors
* Disabled submit button during mutation

### Schema

```tsx
import { z } from 'zod';

export const employeeSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(2, 'Full name must contain at least 2 characters'),

  email: z
    .string()
    .trim()
    .email('Enter a valid email address'),

  departmentId: z
    .string()
    .min(1, 'Department is required'),
});

export type EmployeeFormValues = z.infer<
  typeof employeeSchema
>;
```

### Form

```tsx
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

interface EmployeeFormProps {
  defaultValues?: EmployeeFormValues;
  onSubmit: (values: EmployeeFormValues) => void;
  isSubmitting?: boolean;
  apiError?: string | null;
}

export function EmployeeForm({
  defaultValues,
  onSubmit,
  isSubmitting = false,
  apiError,
}: EmployeeFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EmployeeFormValues>({
    resolver: zodResolver(employeeSchema),
    defaultValues: defaultValues ?? {
      fullName: '',
      email: '',
      departmentId: '',
    },
  });

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4"
    >
      {apiError && (
        <div
          role="alert"
          className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
        >
          {apiError}
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="fullName">Full name</Label>

        <Input
          id="fullName"
          {...register('fullName')}
          aria-invalid={Boolean(errors.fullName)}
          aria-describedby={
            errors.fullName
              ? 'fullName-error'
              : undefined
          }
        />

        {errors.fullName && (
          <p
            id="fullName-error"
            className="text-sm text-destructive"
          >
            {errors.fullName.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>

        <Input
          id="email"
          type="email"
          {...register('email')}
          aria-invalid={Boolean(errors.email)}
          aria-describedby={
            errors.email ? 'email-error' : undefined
          }
        />

        {errors.email && (
          <p
            id="email-error"
            className="text-sm text-destructive"
          >
            {errors.email.message}
          </p>
        )}
      </div>

      <Button
        type="submit"
        disabled={isSubmitting}
      >
        {isSubmitting ? 'Saving...' : 'Save'}
      </Button>
    </form>
  );
}
```

---

## 17. API Error Handling

Do not use `any` for API errors.

### `lib/api-error.ts`

```tsx
import axios from 'axios';

interface ApiErrorResponse {
  message?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export function getApiErrorMessage(
  error: unknown,
): string {
  if (axios.isAxiosError<ApiErrorResponse>(error)) {
    return (
      error.response?.data?.message ??
      error.message ??
      'The request could not be completed.'
    );
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'An unexpected error occurred.';
}
```

---

## 18. Loading, Error, and Empty States

Every data page must support:

* Initial loading
* Background refresh
* Error state
* Empty state
* Success state

### Loading

```tsx
if (query.isLoading) {
  return <PageSkeleton />;
}
```

### Error

```tsx
if (query.isError) {
  return (
    <ErrorState
      title="Unable to load employees"
      description="Please try again or contact the administrator."
      onRetry={() => query.refetch()}
    />
  );
}
```

### Empty

```tsx
if (query.data?.data.length === 0) {
  return (
    <EmptyState
      title="No employees found"
      description="Create an employee or change the current filters."
      action={
        <Button onClick={() => setDialogOpen(true)}>
          Add Employee
        </Button>
      }
    />
  );
}
```

Do not display an empty table without explanation.

---

## 19. Error Boundary

React Query errors and React rendering errors are different.

Use inline error handling for API requests and an Error Boundary for unexpected rendering failures.

```tsx
<ErrorBoundary>
  <App />
</ErrorBoundary>
```

Use route-level boundaries for complex modules when appropriate.

An Error Boundary must:

* Show a friendly message
* Provide a retry action
* Avoid exposing stack traces to users
* Log technical details to the monitoring system

---

## 20. Table Pattern

Tables must support enterprise data workflows.

Recommended features:

* Server-side pagination
* Search
* Filtering
* Sorting
* Row actions
* Loading state
* Empty state
* Responsive overflow
* Optional column visibility
* URL-based filter state

### URL pattern

```text
/employees?page=2&pageSize=20&status=active&search=developer
```

### Responsive table container

```tsx
<div className="overflow-x-auto rounded-lg border border-border">
  <Table>
    {/* Table content */}
  </Table>
</div>
```

### Mobile behavior

For important mobile workflows, consider rendering a card list instead of forcing a wide table.

```tsx
<div className="hidden md:block">
  <EmployeeTable />
</div>

<div className="space-y-3 md:hidden">
  <EmployeeCardList />
</div>
```

---

## 21. Search and Filter Pattern

Use debounced search for server requests.

```tsx
const [searchInput, setSearchInput] = useState('');
const debouncedSearch = useDebounce(searchInput, 400);
```

Do not send a request on every keystroke without debounce.

Filters should be:

* Visible
* Resettable
* Reflected in the URL
* Preserved after page refresh
* Included in the React Query key

---

## 22. Dashboard Pattern

### Stat grid

```tsx
<div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
  <StatCard
    title="Open Tickets"
    value={openTicketCount}
    icon={TicketIcon}
  />

  <StatCard
    title="Employees"
    value={employeeCount}
    icon={UsersIcon}
  />
</div>
```

### Chart grid

```tsx
<div className="grid gap-6 xl:grid-cols-2">
  <TicketTrendChart />
  <TicketStatusChart />
</div>
```

### Dashboard rules

* Use `useMemo` for derived chart data
* Wrap charts inside cards
* Every chart must include a title
* Add a description when the metric may be unclear
* Add empty states when chart data is unavailable
* Use semantic chart tokens
* Avoid placing more than two large charts in one row
* Use role-based query conditions through `enabled`

---

## 23. Chart Colors

Use CSS variables instead of hardcoded hexadecimal values.

```tsx
<Bar
  dataKey="value"
  fill="hsl(var(--chart-1))"
  radius={[4, 4, 0, 0]}
/>
```

For multiple chart series:

```tsx
const chartColors = [
  'hsl(var(--chart-1))',
  'hsl(var(--chart-2))',
  'hsl(var(--chart-3))',
  'hsl(var(--chart-4))',
  'hsl(var(--chart-5))',
];
```

All Recharts charts must use:

```tsx
<ResponsiveContainer width="100%" height={280}>
  {/* Chart */}
</ResponsiveContainer>
```

---

## 24. AI Component Standards

AI features must be visually distinct from normal system features.

Use the `ai` color token for:

* AI badges
* AI action buttons
* Thinking indicators
* Generated summaries
* AI recommendations

Do not use AI styling for normal CRUD actions.

### AI answer structure

```tsx
<AIAnswerCard>
  <AIAnswerHeader
    title="AI Helpdesk Assistant"
    isStreaming={isStreaming}
  />

  <AIMessageContent>
    {answer}
  </AIMessageContent>

  <AISourceList sources={sources} />

  <AIFeedbackActions
    onHelpful={handleHelpful}
    onNotHelpful={handleNotHelpful}
    onRegenerate={handleRegenerate}
  />

  <HumanHandoffButton
    onClick={handleEscalation}
  />
</AIAnswerCard>
```

### AI answer requirements

AI responses should support:

* Streaming state
* Thinking indicator
* Source citations
* Source preview
* Copy answer
* Regenerate answer
* Helpful or not helpful feedback
* Human handoff
* Error recovery
* Clear unverified-answer warning where required

### RAG source display

Each source should show:

* Document title
* Relevant excerpt
* Page or section when available
* Relevance or confidence when available
* Action to open the source

Never present an AI-generated answer as verified fact without showing the available source context.

---

## 25. SignalR Integration

Use a single shared SignalR connection.

```tsx
const {
  onNotification,
  onUnreadCount,
  isConnected,
} = useSignalR();
```

Register handlers inside `useEffect`.

```tsx
useEffect(() => {
  return onNotification((notification) => {
    addToast({
      title: notification.title,
      message: notification.message ?? '',
      type: 'info',
    });
  });
}, [onNotification, addToast]);
```

The subscription function should return an unsubscribe function.

### SignalR requirements

* Use one global connection
* Attach JWT through `accessTokenFactory`
* Support automatic reconnect
* Remove handlers during cleanup
* Show connection status where operationally useful
* Avoid creating one connection per component

Recommended retry delays:

```tsx
[0, 2000, 5000, 10000, 30000]
```

---

## 26. State Management Rules

### React Query

Use for:

* API data
* Caching
* Pagination
* Mutations
* Refetching
* Background synchronization

### Zustand

Use for:

* Authentication state
* Toast state
* Sidebar state
* Small application-wide UI state

### Local component state

Use for:

* Dialog visibility
* Temporary form UI
* Selected row
* Local tab selection
* Search input before debounce

Do not place all state into Zustand.

Do not duplicate API data from React Query inside Zustand.

---

## 27. Routing

### Route registration

```tsx
<Route
  path="employees"
  element={<EmployeesPage />}
/>
```

### Navigation registration

```tsx
{
  to: '/employees',
  label: 'Employees',
  icon: Users,
}
```

### Navigation usage

```tsx
const navigate = useNavigate();

navigate('/employees');
navigate(`/employees/${employeeId}`);
```

### Route rules

* Add new routes to `App.tsx`
* Add visible routes to the sidebar or navigation configuration
* Protect restricted routes
* Apply role and permission checks
* Provide a not-found route
* Use lazy loading for large modules when useful

---

## 28. Authorization in the UI

Frontend authorization improves UX but does not replace backend authorization.

```tsx
const canManageEmployees =
  user?.permissions.includes('employees.manage');

{canManageEmployees && (
  <Button>Add Employee</Button>
)}
```

The backend must still validate every protected action.

Use permission-based checks instead of spreading role-name comparisons throughout the application.

Preferred:

```tsx
hasPermission('tickets.assign')
```

Avoid:

```tsx
user.role === 'Admin' || user.role === 'SuperAdmin'
```

unless the business rule explicitly depends on the role itself.

---

## 29. Responsive Design Rules

### Page spacing

```tsx
<div className="p-4 md:p-6">
```

### Responsive header

```tsx
<div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
```

### Responsive forms

```tsx
<div className="grid gap-4 md:grid-cols-2">
```

### Responsive dashboard

```tsx
<div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
```

### Mobile requirements

* Sidebar becomes a drawer
* Forms become one column
* Tables scroll horizontally or become card lists
* Primary mobile actions remain easy to reach
* Dialogs must fit the viewport
* Touch targets should be at least 44 × 44 px
* Avoid hover-only interactions
* Long text must wrap correctly
* Buttons may become full width on small screens

---

## 30. Accessibility

Accessibility is mandatory.

### Labels

Every input must have a label.

```tsx
<Label htmlFor="email">
  Email
</Label>

<Input
  id="email"
  type="email"
/>
```

### Icon-only buttons

```tsx
<Button
  variant="ghost"
  size="icon"
  aria-label="Delete employee"
>
  <Trash2 className="h-4 w-4" />
  <span className="sr-only">
    Delete employee
  </span>
</Button>
```

### Form errors

```tsx
<Input
  id="email"
  aria-invalid={Boolean(errors.email)}
  aria-describedby={
    errors.email ? 'email-error' : undefined
  }
/>

{errors.email && (
  <p
    id="email-error"
    className="text-sm text-destructive"
  >
    {errors.email.message}
  </p>
)}
```

### Loading indicators

```tsx
<Loader2
  className="h-4 w-4 animate-spin"
  role="status"
  aria-label="Loading"
/>
```

### Clickable cards

Prefer a semantic link or button.

```tsx
<Link
  to="/tickets"
  className="block rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
>
  <Card>
    {/* Content */}
  </Card>
</Link>
```

When a card cannot use a semantic link:

```tsx
<Card
  role="button"
  tabIndex={0}
  onClick={handleClick}
  onKeyDown={(event) => {
    if (
      event.key === 'Enter' ||
      event.key === ' '
    ) {
      handleClick();
    }
  }}
/>
```

### Accessibility requirements

* Preserve Radix UI ARIA attributes
* Ensure sufficient contrast
* Support keyboard navigation
* Show visible focus styles
* Do not communicate status through color alone
* Use semantic HTML
* Associate error messages with inputs
* Use `aria-live` for dynamic AI and notification updates when needed

---

## 31. Dialog Standards

Use Radix Dialog for modal workflows.

Dialogs must include:

* Title
* Optional description
* Close button
* Keyboard support
* Focus trap
* Clear primary and secondary actions
* Loading state during submission

```tsx
<DialogContent className="sm:max-w-lg">
  <DialogHeader>
    <DialogTitle>
      Add Employee
    </DialogTitle>

    <DialogDescription>
      Create an employee account and assign a department.
    </DialogDescription>
  </DialogHeader>

  <EmployeeForm />

  <DialogFooter>
    <Button
      type="button"
      variant="outline"
      onClick={() => setOpen(false)}
    >
      Cancel
    </Button>

    <Button
      type="submit"
      form="employee-form"
    >
      Save
    </Button>
  </DialogFooter>
</DialogContent>
```

Use a drawer or bottom sheet for mobile-heavy workflows where appropriate.

---

## 32. Toast Standards

Use toast messages for short operation feedback.

Good examples:

```text
Employee created successfully.
Ticket assigned to Ahmad.
Article saved as draft.
```

Avoid vague messages:

```text
Success.
Done.
Operation completed.
```

Use inline errors when the user must correct something.

Do not rely only on toast messages for form validation.

---

## 33. File Upload Pattern

```tsx
const formData = new FormData();
formData.append('file', file);

await api.post('/knowledge-documents', formData, {
  headers: {
    'Content-Type': 'multipart/form-data',
  },
});
```

File upload UI should show:

* Accepted file types
* Maximum file size
* Selected file name
* Upload progress when available
* Validation errors
* Upload success
* Option to remove or replace the file

For knowledge base uploads, also show:

* Indexing status
* Chunking status
* Embedding status
* Ready or failed state

---

## 34. Dark Mode

Dark mode uses the `.dark` class on the root HTML element.

Use semantic tokens whenever possible:

```tsx
<div className="bg-background text-foreground" />
```

Use `dark:` only for one-off cases that cannot be represented by tokens.

```tsx
<div className="bg-white dark:bg-slate-950" />
```

Before committing, verify:

* Text contrast
* Borders
* Dialog surfaces
* Input backgrounds
* Charts
* Status badges
* AI cards
* Hover states
* Focus states

---

## 35. Performance Rules

* Use route-level lazy loading for large modules
* Use `useMemo` only for meaningful derived calculations
* Avoid unnecessary global state
* Debounce server search
* Use server-side pagination for large datasets
* Use image lazy loading
* Avoid rendering thousands of rows at once
* Consider virtualization for very large tables
* Keep React Query keys stable
* Do not refetch data unnecessarily
* Avoid premature optimization

---

## 36. Playwright End-to-End Testing

Tests are stored in:

```text
frontend/tests/e2e/
```

Basic example:

```tsx
import {
  test,
  expect,
} from '@playwright/test';

test(
  'employees page loads correctly',
  async ({ page }) => {
    await page.goto('/employees');

    await expect(
      page.getByRole('heading', {
        name: 'Employees',
      }),
    ).toBeVisible();
  },
);
```

Form test:

```tsx
test(
  'user can create an employee',
  async ({ page }) => {
    await page.goto('/employees');

    await page
      .getByRole('button', {
        name: 'Add Employee',
      })
      .click();

    await page
      .getByLabel('Full name')
      .fill('Ahmad Fauzan');

    await page
      .getByLabel('Email')
      .fill('ahmad@example.com');

    await page
      .getByRole('button', {
        name: 'Save',
      })
      .click();

    await expect(
      page.getByText(
        'Employee created successfully.',
      ),
    ).toBeVisible();
  },
);
```

Run tests:

```bash
npm run test:e2e
```

or:

```bash
npx playwright test
```

### Testing requirements

Prioritize testing:

* Login
* Protected routes
* Main navigation
* CRUD operations
* Form validation
* Search and filters
* Pagination
* Role restrictions
* AI prompt submission
* AI streaming response
* Source display
* Human handoff
* Real-time notifications
* Mobile navigation

---

## 37. Definition of Done

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

## 38. Pre-Commit Checklist

### Architecture

* [ ] Components are placed in the correct directory
* [ ] Reusable business components are extracted appropriately
* [ ] API calls use the service layer
* [ ] API data is managed by React Query
* [ ] Local UI state is not unnecessarily global
* [ ] Imports use the `@/` alias
* [ ] Components use named exports

### TypeScript

* [ ] No unnecessary `any`
* [ ] API responses are typed
* [ ] Form values are inferred from Zod
* [ ] Mutation errors use `unknown` or typed Axios errors
* [ ] `npm run build` passes

### Styling

* [ ] Semantic design tokens are used
* [ ] No unnecessary hardcoded colors
* [ ] `cn()` is used for conditional classes
* [ ] Dark mode is verified
* [ ] Mobile layout is verified
* [ ] AI features use the AI visual token
* [ ] Status colors are centralized

### Forms

* [ ] React Hook Form is used
* [ ] Zod validation is used
* [ ] Every input has a label
* [ ] Errors use `aria-describedby`
* [ ] Submit is disabled during submission
* [ ] API errors are visible
* [ ] Edit forms receive default values

### Data Pages

* [ ] Loading state is handled
* [ ] Error state is handled
* [ ] Empty state is handled
* [ ] Pagination is implemented for large datasets
* [ ] Search is debounced
* [ ] Filters are reflected in the URL
* [ ] Query keys contain all active filters

### Accessibility

* [ ] Icon-only buttons have accessible labels
* [ ] Keyboard navigation works
* [ ] Focus states are visible
* [ ] Dialog focus is managed correctly
* [ ] Status is not communicated only through color
* [ ] Dynamic updates use appropriate ARIA behavior

### AI Features

* [ ] Streaming state is visible
* [ ] Sources are displayed
* [ ] AI errors can be retried
* [ ] Feedback controls are available
* [ ] Human handoff is available where required
* [ ] Unverified answers are clearly identified

### Testing

* [ ] Main user flow has a Playwright test
* [ ] Validation behavior is tested
* [ ] Permission behavior is tested
* [ ] No browser console errors occur
* [ ] Existing tests still pass

---

## 39. Final Principles

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
