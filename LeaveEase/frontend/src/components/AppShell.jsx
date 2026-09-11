import { useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext.jsx'
import Icon from './Icon.jsx'

const pageTitles = {
  '/': ['Overview', 'Your leave activity and balances at a glance.'],
  '/apply': ['Apply for leave', 'Submit a new leave request for manager review.'],
  '/my-leaves': ['My leave requests', 'Track current and previous leave requests.'],
  '/approvals': ['Team approvals', 'Review pending requests from your team.'],
  '/admin': ['Administration', 'Manage leave policies, allocations, and employees.'],
}

export default function AppShell({ children }) {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [mobileOpen, setMobileOpen] = useState(false)
  const isManager = user?.roles?.includes('Manager') || user?.roles?.includes('Admin')
  const isAdmin = user?.roles?.includes('Admin')
  const [title, subtitle] = pageTitles[location.pathname] || pageTitles['/']

  const navItems = [
    { to: '/', label: 'Overview', icon: 'dashboard', end: true },
    { to: '/apply', label: 'Apply leave', icon: 'plus' },
    { to: '/my-leaves', label: 'My requests', icon: 'calendar' },
    ...(isManager ? [{ to: '/approvals', label: 'Team approvals', icon: 'check' }] : []),
    ...(isAdmin ? [{ to: '/admin', label: 'Administration', icon: 'settings' }] : []),
  ]

  const onLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="app-shell">
      {mobileOpen && <button className="sidebar-scrim" aria-label="Close menu" onClick={() => setMobileOpen(false)} />}
      <aside className={`sidebar ${mobileOpen ? 'sidebar-open' : ''}`}>
        <div className="brand-wrap">
          <div className="brand-mark">L</div>
          <div><div className="brand-name">LeaveEase</div><div className="brand-sub">People workspace</div></div>
        </div>
        <nav className="sidebar-nav">
          <span className="nav-caption">Workspace</span>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `nav-link ${isActive ? 'nav-active' : ''}`}
              onClick={() => setMobileOpen(false)}
            >
              <Icon name={item.icon} size={19} />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-user">
          <div className="avatar">{initials(user?.fullName)}</div>
          <div className="user-mini">
            <strong>{user?.fullName}</strong>
            <span>{user?.jobTitle}</span>
          </div>
          <button className="icon-button subtle" onClick={onLogout} title="Sign out"><Icon name="logout" size={18} /></button>
        </div>
      </aside>

      <main className="app-main">
        <header className="topbar">
          <button className="mobile-menu icon-button" onClick={() => setMobileOpen(true)} aria-label="Open menu"><Icon name="menu" /></button>
          <div>
            <h1>{title}</h1>
            <p>{subtitle}</p>
          </div>
          <div className="topbar-actions">
            <div className="role-pill">{user?.roles?.[0] || 'Employee'}</div>
          </div>
        </header>
        <div className="page-content">{children}</div>
      </main>
    </div>
  )
}

function initials(name = '') {
  return name.split(' ').filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase() || 'U'
}
