import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiGet } from '../api/client.js'
import StatCard from '../components/StatCard.jsx'
import StatusBadge from '../components/StatusBadge.jsx'
import EmptyState from '../components/EmptyState.jsx'
import Icon from '../components/Icon.jsx'
import { daysLabel, formatDate } from '../utils.js'
import { useAuth } from '../auth/AuthContext.jsx'

export default function DashboardPage() {
  const { hasRole } = useAuth()
  const [data, setData] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    apiGet('/dashboard/summary').then(setData).catch((e) => setError(e.message))
  }, [])

  if (error) return <div className="alert error"><Icon name="alert" size={18} />{error}</div>
  if (!data) return <DashboardSkeleton />

  return (
    <div className="stack-lg">
      <section className="welcome-strip">
        <div>
          <span className="eyebrow">{data.currentYear} LEAVE YEAR</span>
          <h2>Good to see you, {data.greetingName}.</h2>
          <p>Your leave balances and recent activity are up to date.</p>
        </div>
        <Link to="/apply" className="primary-button"><Icon name="plus" size={18} /> Apply for leave</Link>
      </section>

      <section className="stats-grid">
        <StatCard label="Available balance" value={daysLabel(data.totalAvailableDays)} helper="Across active leave types" icon="calendar" tone="indigo" />
        <StatCard label="Used this year" value={daysLabel(data.totalUsedDays)} helper={`${data.approvedRequests} approved requests`} icon="check" tone="green" />
        <StatCard label="Awaiting decision" value={daysLabel(data.totalPendingDays)} helper={`${data.pendingRequests} request${data.pendingRequests === 1 ? '' : 's'} pending`} icon="clock" tone="amber" />
        {(hasRole('Manager') || hasRole('Admin')) && (
          <StatCard label="Team approvals" value={data.teamPendingApprovals} helper="Requests waiting for you" icon="users" tone="blue" />
        )}
      </section>

      <section className="content-grid two-thirds">
        <article className="panel">
          <div className="panel-header">
            <div><h2>Leave balances</h2><p>Allocation and usage for {data.currentYear}</p></div>
            <Link className="text-link" to="/apply">Request leave <Icon name="arrow" size={15} /></Link>
          </div>
          <div className="balance-grid">
            {data.balances.map((balance) => {
              const percent = balance.allocated > 0 ? Math.min(100, (balance.used + balance.pending) / balance.allocated * 100) : 0
              return (
                <div className="balance-card" key={balance.id}>
                  <div className="balance-card-head">
                    <div className="type-dot" style={{ background: balance.color }} />
                    <div><strong>{balance.leaveType}</strong><span>{balance.code}</span></div>
                    <b>{balance.available}</b>
                  </div>
                  <div className="progress-track"><span style={{ width: `${percent}%`, background: balance.color }} /></div>
                  <div className="balance-meta"><span>{balance.used} used</span><span>{balance.pending} pending</span><span>{balance.allocated} allocated</span></div>
                </div>
              )
            })}
          </div>
        </article>

        <article className="panel side-panel">
          <div className="panel-header"><div><h2>At a glance</h2><p>Request outcomes</p></div></div>
          <div className="outcome-list">
            <OutcomeRow label="Pending" value={data.pendingRequests} tone="amber" />
            <OutcomeRow label="Approved" value={data.approvedRequests} tone="green" />
            <OutcomeRow label="Rejected" value={data.rejectedRequests} tone="red" />
          </div>
          {(hasRole('Manager') || hasRole('Admin')) && data.teamPendingApprovals > 0 && (
            <Link to="/approvals" className="approval-callout">
              <span className="callout-icon"><Icon name="users" /></span>
              <span><strong>{data.teamPendingApprovals} team request{data.teamPendingApprovals === 1 ? '' : 's'} pending</strong><small>Open approval queue</small></span>
              <Icon name="arrow" size={18} />
            </Link>
          )}
        </article>
      </section>

      <article className="panel">
        <div className="panel-header">
          <div><h2>Recent requests</h2><p>Your latest leave activity</p></div>
          <Link className="text-link" to="/my-leaves">View all <Icon name="arrow" size={15} /></Link>
        </div>
        {data.recentRequests.length === 0 ? (
          <EmptyState title="No leave requests yet" description="When you submit a leave request, it will appear here." action={<Link className="secondary-button" to="/apply">Create first request</Link>} />
        ) : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Leave type</th><th>Dates</th><th>Duration</th><th>Status</th><th>Submitted</th></tr></thead>
              <tbody>
                {data.recentRequests.map((request) => (
                  <tr key={request.id}>
                    <td><div className="type-cell"><span className="type-dot" style={{ background: request.leaveTypeColor }} /><div><strong>{request.leaveType}</strong><small>#{request.id}</small></div></div></td>
                    <td>{formatDate(request.startDate)} <span className="date-sep">→</span> {formatDate(request.endDate)}</td>
                    <td>{daysLabel(request.totalDays)}</td>
                    <td><StatusBadge status={request.status} /></td>
                    <td className="muted-cell">{formatDate(request.createdAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>
    </div>
  )
}

function OutcomeRow({ label, value, tone }) {
  return <div className="outcome-row"><span><i className={`outcome-dot ${tone}`} />{label}</span><strong>{value}</strong></div>
}

function DashboardSkeleton() {
  return <div className="stack-lg"><div className="skeleton hero-skeleton" /><div className="stats-grid"><div className="skeleton stat-skeleton" /><div className="skeleton stat-skeleton" /><div className="skeleton stat-skeleton" /></div><div className="skeleton table-skeleton" /></div>
}
