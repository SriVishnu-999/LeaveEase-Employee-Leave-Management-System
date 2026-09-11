import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { apiGet, apiPut } from '../api/client.js'
import StatusBadge from '../components/StatusBadge.jsx'
import EmptyState from '../components/EmptyState.jsx'
import Modal from '../components/Modal.jsx'
import Icon from '../components/Icon.jsx'
import { daysLabel, formatDate, formatDateTime } from '../utils.js'

export default function MyLeavesPage() {
  const location = useLocation()
  const [items, setItems] = useState([])
  const [filter, setFilter] = useState('All')
  const [selected, setSelected] = useState(null)
  const [message, setMessage] = useState(location.state?.message || '')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    apiGet('/leave-requests/mine')
      .then(setItems)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const filtered = useMemo(() => filter === 'All' ? items : items.filter((x) => x.status === filter), [items, filter])

  const openDetails = async (request) => {
    try {
      setSelected(await apiGet(`/leave-requests/${request.id}`))
    } catch (e) {
      setError(e.message)
    }
  }

  const cancelRequest = async () => {
    if (!selected) return
    try {
      await apiPut(`/leave-requests/${selected.id}/cancel`)
      setSelected(null)
      setMessage('Pending leave request cancelled.')
      load()
    } catch (e) {
      setError(e.message)
    }
  }

  return (
    <div className="stack-lg">
      {message && <div className="alert success"><Icon name="check" size={18} />{message}<button onClick={() => setMessage('')}>×</button></div>}
      {error && <div className="alert error"><Icon name="alert" size={18} />{error}</div>}

      <div className="toolbar">
        <div className="segmented-control">
          {['All', 'Pending', 'Approved', 'Rejected', 'Cancelled'].map((value) => (
            <button key={value} className={filter === value ? 'active' : ''} onClick={() => setFilter(value)}>{value}</button>
          ))}
        </div>
        <Link className="primary-button" to="/apply"><Icon name="plus" size={17} /> New request</Link>
      </div>

      <article className="panel flush-panel">
        {loading ? <div className="skeleton table-skeleton" /> : filtered.length === 0 ? (
          <EmptyState title="No requests found" description={filter === 'All' ? 'Submit a leave request to start tracking it here.' : `You have no ${filter.toLowerCase()} requests.`} action={filter === 'All' ? <Link className="secondary-button" to="/apply">Apply for leave</Link> : null} />
        ) : (
          <div className="table-wrap">
            <table className="clickable-table">
              <thead><tr><th>Request</th><th>Dates</th><th>Duration</th><th>Status</th><th>Manager comment</th><th>Submitted</th><th /></tr></thead>
              <tbody>
                {filtered.map((request) => (
                  <tr key={request.id} onClick={() => openDetails(request)}>
                    <td><div className="type-cell"><span className="type-dot" style={{ background: request.leaveTypeColor }} /><div><strong>{request.leaveType}</strong><small>#{request.id}</small></div></div></td>
                    <td>{formatDate(request.startDate)} <span className="date-sep">→</span> {formatDate(request.endDate)}</td>
                    <td>{daysLabel(request.totalDays)}</td>
                    <td><StatusBadge status={request.status} /></td>
                    <td className="truncate-cell">{request.managerComment || '—'}</td>
                    <td className="muted-cell">{formatDate(request.createdAtUtc)}</td>
                    <td><Icon name="arrow" size={17} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>

      <Modal open={Boolean(selected)} title={selected ? `Leave request #${selected.id}` : ''} onClose={() => setSelected(null)}>
        {selected && (
          <div className="detail-stack">
            <div className="detail-title-row"><div className="type-cell"><span className="type-dot large" style={{ background: selected.leaveTypeColor }} /><div><strong>{selected.leaveType}</strong><small>{selected.leaveTypeCode}</small></div></div><StatusBadge status={selected.status} /></div>
            <div className="detail-grid">
              <div><span>Start date</span><strong>{formatDate(selected.startDate)}</strong></div>
              <div><span>End date</span><strong>{formatDate(selected.endDate)}</strong></div>
              <div><span>Duration</span><strong>{daysLabel(selected.totalDays)}</strong></div>
              <div><span>Submitted</span><strong>{formatDateTime(selected.createdAtUtc)}</strong></div>
            </div>
            <div className="detail-section"><span>Reason</span><p>{selected.reason}</p></div>
            {selected.managerComment && <div className="comment-box"><strong>Manager comment</strong><p>{selected.managerComment}</p></div>}
            {selected.history?.length > 0 && (
              <div className="detail-section"><span>Activity</span><div className="timeline">{selected.history.map((h) => <div className="timeline-item" key={h.id}><i /><div><strong>{h.toStatus}</strong><p>{h.comment || `Status changed to ${h.toStatus}`}</p><small>{h.changedBy} · {formatDateTime(h.changedAtUtc)}</small></div></div>)}</div></div>
            )}
            {selected.status === 'Pending' && <div className="modal-actions"><button className="danger-button" onClick={cancelRequest}>Cancel request</button><button className="secondary-button" onClick={() => setSelected(null)}>Close</button></div>}
          </div>
        )}
      </Modal>
    </div>
  )
}
