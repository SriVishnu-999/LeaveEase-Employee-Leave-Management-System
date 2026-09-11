import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiGet, apiPost } from '../api/client.js'
import Icon from '../components/Icon.jsx'
import { daysLabel, todayIso } from '../utils.js'

export default function ApplyLeavePage() {
  const navigate = useNavigate()
  const [types, setTypes] = useState([])
  const [balances, setBalances] = useState([])
  const [form, setForm] = useState({ leaveTypeId: '', startDate: '', endDate: '', reason: '' })
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    Promise.all([apiGet('/leave-types'), apiGet('/leave-balances/me')])
      .then(([t, b]) => { setTypes(t); setBalances(b); if (t.length) setForm((f) => ({ ...f, leaveTypeId: String(t[0].id) })) })
      .catch((e) => setError(e.message))
  }, [])

  const selectedBalance = balances.find((x) => x.leaveTypeId === Number(form.leaveTypeId))
  const estimatedDays = useMemo(() => countWeekdays(form.startDate, form.endDate), [form.startDate, form.endDate])
  const overBalance = selectedBalance && estimatedDays > selectedBalance.available

  const submit = async (event) => {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await apiPost('/leave-requests', {
        leaveTypeId: Number(form.leaveTypeId),
        startDate: form.startDate,
        endDate: form.endDate,
        reason: form.reason,
      })
      navigate('/my-leaves', { state: { message: 'Leave request submitted successfully.' } })
    } catch (e) {
      setError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="content-grid form-layout">
      <article className="panel form-panel">
        <div className="panel-header"><div><h2>Leave details</h2><p>Working days exclude Saturdays and Sundays.</p></div></div>
        {error && <div className="alert error"><Icon name="alert" size={18} />{error}</div>}
        <form className="leave-form" onSubmit={submit}>
          <label className="field full-field">
            <span>Leave type</span>
            <select value={form.leaveTypeId} onChange={(e) => setForm({ ...form, leaveTypeId: e.target.value })} required>
              {types.map((type) => <option key={type.id} value={type.id}>{type.name} ({type.code})</option>)}
            </select>
            {selectedBalance && <small>{daysLabel(selectedBalance.available)} currently available</small>}
          </label>
          <label className="field">
            <span>Start date</span>
            <input type="date" min={todayIso()} value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value, endDate: form.endDate && form.endDate < e.target.value ? e.target.value : form.endDate })} required />
          </label>
          <label className="field">
            <span>End date</span>
            <input type="date" min={form.startDate || todayIso()} value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} required />
          </label>
          <label className="field full-field">
            <span>Reason</span>
            <textarea rows="5" maxLength="1000" minLength="5" placeholder="Briefly explain the reason for your leave..." value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} required />
            <small className="char-count">{form.reason.length}/1000</small>
          </label>

          <div className={`request-summary ${overBalance ? 'summary-error' : ''}`}>
            <div><span>Estimated duration</span><strong>{daysLabel(estimatedDays)}</strong></div>
            <div><span>Balance after request</span><strong>{selectedBalance ? daysLabel(Math.max(0, selectedBalance.available - estimatedDays)) : '—'}</strong></div>
          </div>
          {overBalance && <div className="inline-warning"><Icon name="alert" size={17} /> This request exceeds your available balance.</div>}

          <div className="form-actions">
            <button type="button" className="secondary-button" onClick={() => navigate(-1)}>Cancel</button>
            <button type="submit" className="primary-button" disabled={submitting || overBalance || estimatedDays <= 0}>
              {submitting ? <span className="spinner" /> : <Icon name="mail" size={17} />}
              {submitting ? 'Submitting...' : 'Submit request'}
            </button>
          </div>
        </form>
      </article>

      <aside className="stack-md">
        <article className="panel compact-panel">
          <h3>Your balances</h3>
          <p className="muted">Current-year availability</p>
          <div className="mini-balance-list">
            {balances.map((balance) => (
              <div key={balance.id} className="mini-balance-row">
                <span><i style={{ background: balance.color }} />{balance.leaveType}</span>
                <strong>{balance.available}</strong>
              </div>
            ))}
          </div>
        </article>
        <article className="info-card">
          <span className="info-icon"><Icon name="briefcase" /></span>
          <div><strong>How approval works</strong><p>Your request is sent to your assigned manager. You’ll receive an email when its status changes.</p></div>
        </article>
      </aside>
    </div>
  )
}

function countWeekdays(start, end) {
  if (!start || !end || end < start) return 0
  let count = 0
  const date = new Date(`${start}T00:00:00`)
  const final = new Date(`${end}T00:00:00`)
  while (date <= final) {
    const day = date.getDay()
    if (day !== 0 && day !== 6) count += 1
    date.setDate(date.getDate() + 1)
  }
  return count
}
