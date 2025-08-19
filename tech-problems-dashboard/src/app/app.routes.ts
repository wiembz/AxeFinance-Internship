import { Routes } from '@angular/router';
import { authGuard, superadminSignupGuard } from './core/auth/guards';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'superadmin-signup', canActivate: [superadminSignupGuard], loadComponent: () => import('./features/auth/superadmin-signup/superadmin-signup.component').then(m => m.SuperadminSignupComponent) },
  {
    path: '',
    canActivate: [authGuard],
    component: MainLayoutComponent,
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      // Admin routes
      {
        path: 'admin',
        children: [
          // Department management
          {
            path: 'departments',
            loadComponent: () => import('./features/departments/department-list/department-list.component').then(m => m.DepartmentListComponent)
          },
          {
            path: 'departments/create',
            loadComponent: () => import('./features/departments/department-form/department-form.component').then(m => m.DepartmentFormComponent)
          },
          {
            path: 'departments/edit/:id',
            loadComponent: () => import('./features/departments/department-form/department-form.component').then(m => m.DepartmentFormComponent)
          },
          {
            path: 'departments/:departmentId/projects',
            loadComponent: () => import('./features/projects/project-list/project-list.component').then(m => m.ProjectListComponent)
          },
          // Project management
          {
            path: 'projects',
            loadComponent: () => import('./features/projects/project-list/project-list.component').then(m => m.ProjectListComponent)
          },
          {
            path: 'projects/create',
            loadComponent: () => import('./features/projects/project-form/project-form.component').then(m => m.ProjectFormComponent)
          },
          {
            path: 'projects/edit/:id',
            loadComponent: () => import('./features/projects/project-form/project-form.component').then(m => m.ProjectFormComponent)
          },
          {
            path: 'projects/department/:departmentId',
            loadComponent: () => import('./features/projects/project-list/project-list.component').then(m => m.ProjectListComponent)
          },
          {
            path: 'projects/:projectId/problems',
            loadComponent: () => import('./features/problems/problem-list/problem-list.component').then(m => m.ProblemListComponent)
          }
        ]
      },
      // Problems routes
      {
        path: 'problems',
        children: [
          {
            path: '',
            loadComponent: () => import('./features/problems/problem-list/problem-list.component').then(m => m.ProblemListComponent)
          },
          {
            path: 'submit',
            loadComponent: () => import('./features/problems/problem-submit/problem-submit.component').then(m => m.ProblemSubmitComponent)
          },
          {
            path: 'project/:projectId',
            loadComponent: () => import('./features/problems/problem-list/problem-list.component').then(m => m.ProblemListComponent)
          }
        ]
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
