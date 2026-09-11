import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext.jsx'
import AppShell from './components/AppShell.jsx'
import LoginPage from './pages/LoginPage.jsx'
import DashboardPage from './pages/DashboardPage.jsx'
import ApplyLeavePage from './pages/ApplyLeavePage.jsx'
import MyLeavesPage from './pages/MyLeavesPage.jsx'
import TeamApprovalsPage from './pages/TeamApprovalsPage.jsx'
import AdminPage from './pages/AdminPage.jsx'

function ProtectedRoute({ children, roles }) {
  const { user } = useAuth()
  const token = localStorage.getItem('leaveease_token')
  if (!user || !token) return <Navigate to="/login" replace />
  if (roles && !roles.some((role) => user.roles?.includes(role))) {
    return <Navigate to="/" replace />
  }
  return children
}

export default function App() {
  const { user } = useAuth()

  return (
    <Routes>
      <Route path="/login" element={user ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <AppShell>
              <Routes>
                <Route index element={<DashboardPage />} />
                <Route path="apply" element={<ApplyLeavePage />} />
                <Route path="my-leaves" element={<MyLeavesPage />} />
                <Route
                  path="approvals"
                  element={
                    <ProtectedRoute roles={['Manager', 'Admin']}>
                      <TeamApprovalsPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="admin"
                  element={
                    <ProtectedRoute roles={['Admin']}>
                      <AdminPage />
                    </ProtectedRoute>
                  }
                />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </AppShell>
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}
