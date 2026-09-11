import { useEffect, useState } from 'react'
import { apiGet, apiPut } from '../api/client.js'
import EmptyState from '../components/EmptyState.jsx'
import Modal from '../components/Modal.jsx'
import Icon from '../components/Icon.jsx'
import { daysLabel, formatDate, initials } from '../utils.js'

export default function TeamApprovalsPage() {
  const [items, setItems] = useState([])
  const [selected, setSelected] = useState(null)
  const [decision, setDecision] = useState('Approved')
  const [comment, setComment] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)

  const load = () => {
    setLoading(true)
    apiGet('/leave-requests/pending').then(setItems).catch((e) => setError(e.message)).finally(() => setLoading(false))
  }
  useEffect(load, [])

  const choose = (request, value) => { setSelected(request); setDecision(value); setComment(value === 'Approved' ? 'Approved. Please ensure handover is completed.' : '') }

  const submitDecision = async () => {
    if (!selected) return
    setSubmitting(true); setError('')
    try {
      await apiPut(`/leave-requests/${selected.id}/decision`, { status: decision, comment })
      setSelected(null); setComment(''); load()
    } catch (e) {
      setError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="stack-lg">
      {error && <div className="alert error"><Icon name="alert" size={18} />{error}</div>}
      <section className="approval-summary">
        <div><span className="eyebrow">REVIEW QUEUE</span><h2>{items.length} request{items.length === 1 ? '' : 's'} waiting</h2><p>Review dates, available context, and employee reason before making a decision.</p></div>
        <div className="queue-badge"><Icon name="clock" /><strong>{items.length}</strong><span>Pending</span></div>
      </section>

      {loading ? <div className="skeleton table-skeleton" /> : items.length === 0 ? (
        <article className="panel"><EmptyState icon="check" title="You're all caught up" description="There are no pending team leave requests to review." /></article>
      ) : (
        <div className="approval-list">
          {items.map((request) => (
            <article className="approval-card" key={request.id}>
              <div className="approval-person">
                <div className="avatar large-avatar">{initials(request.employeeName)}</div>
                <div><strong>{request.employeeName}</strong><span>{request.department}</span></div>
              </div>
              <div className="approval-main">
                <div className="approval-title"><span className="type-dot" style={{ background: request.leaveTypeColor }} /><strong>{request.leaveType}</strong><small>Request #{request.id}</small></div>
                <div className="approval-dates"><span><small>From</small><strong>{formatDate(request.startDate)}</strong></span><span className="date-arrow">→</span><span><small>To</small><strong>{formatDate(request.endDate)}</strong></span><span className="duration-chip">{daysLabel(request.totalDays)}</span></div>
                <p className="reason-preview">“{request.reason}”</p>
              </div>
              <div className="approval-actions"><button className="secondary-button reject-button" onClick={() => choose(request, 'Rejected')}>Reject</button><button className="primary-button approve-button" onClick={() => choose(request, 'Approved')}><Icon name="check" size={16} /> Approve</button></div>
            </article>
          ))}
        </div>
      )}

      <Modal open={Boolean(selected)} title={`${decision === 'Approved' ? 'Approve' : 'Reject'} leave request`} onClose={() => setSelected(null)}>
        {selected && <div className="decision-form">
          <div className="decision-request"><div className="avatar">{initials(selected.employeeName)}</div><div><strong>{selected.employeeName}</strong><span>{selected.leaveType} · {formatDate(selected.startDate)} – {formatDate(selected.endDate)} · {daysLabel(selected.totalDays)}</span></div></div>
          <label className="field"><span>{decision === 'Rejected' ? 'Reason for rejection' : 'Comment (optional)'}</span><textarea rows="4" value={comment} onChange={(e) => setComment(e.target.value)} placeholder={decision === 'Rejected' ? 'Explain why this request cannot be approved...' : 'Add a note for the employee...'} /></label>
          <div className="modal-actions"><button className="secondary-button" onClick={() => setSelected(null)}>Back</button><button className={decision === 'Rejected' ? 'danger-button' : 'primary-button'} onClick={submitDecision} disabled={submitting || (decision === 'Rejected' && !comment.trim())}>{submitting ? 'Saving...' : `Confirm ${decision.toLowerCase()}`}</button></div>
        </div>}
      </Modal>
    </div>
  )
}
