import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext.jsx'
import Icon from '../components/Icon.jsx'

const demos = [
  { label: 'Employee', email: 'employee@leaveease.local' },
  { label: 'Manager', email: 'manager@leaveease.local' },
  { label: 'Admin', email: 'admin@leaveease.local' },
]

export default function LoginPage() {
  const { login, loading } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('employee@leaveease.local')
  const [password, setPassword] = useState('Pass@123')
  const [error, setError] = useState('')

  const submit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      setError(err.message || 'Unable to sign in.')
    }
  }

  return (
    <div className="login-layout">
      <section className="login-visual">
        <div className="login-visual-inner">
          <div className="login-brand"><div className="brand-mark light">L</div><span>LeaveEase</span></div>
          <div className="login-copy">
            <span className="eyebrow light-text">SMARTER PEOPLE OPERATIONS</span>
            <h1>Leave management that feels effortless.</h1>
            <p>One workspace for balances, requests, approvals, and clear communication across your team.</p>
          </div>
          <div className="visual-card-grid">
            <div className="glass-card">
              <span className="glass-icon"><Icon name="calendar" /></span>
              <strong>18 days</strong><small>Annual balance</small>
            </div>
            <div className="glass-card offset">
              <span className="glass-icon"><Icon name="check" /></span>
              <strong>Fast approvals</strong><small>Clear manager workflow</small>
            </div>
          </div>
          <div className="login-footer-note"><span className="pulse-dot" /> Secure role-based workspace</div>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-form-wrap">
          <div className="mobile-login-brand"><div className="brand-mark">L</div><span>LeaveEase</span></div>
          <span className="eyebrow">WELCOME BACK</span>
          <h2>Sign in to your workspace</h2>
          <p className="muted">Use your company account to access LeaveEase.</p>

          {error && <div className="alert error"><Icon name="alert" size={18} /><span>{error}</span></div>}

          <form onSubmit={submit} className="auth-form">
            <label>
              <span>Email address</span>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
            </label>
            <label>
              <span>Password</span>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="current-password" />
            </label>
            <button className="primary-button full" type="submit" disabled={loading}>
              {loading ? <span className="spinner" /> : 'Sign in'}
              {!loading && <Icon name="arrow" size={17} />}
            </button>
          </form>

          <div className="demo-box">
            <div><strong>Demo access</strong><span>Password: Pass@123</span></div>
            <div className="demo-buttons">
              {demos.map((demo) => (
                <button key={demo.label} type="button" onClick={() => { setEmail(demo.email); setPassword('Pass@123') }}>
                  {demo.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </section>
    </div>
  )
}
