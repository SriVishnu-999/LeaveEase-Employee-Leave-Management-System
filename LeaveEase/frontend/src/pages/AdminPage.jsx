import { useEffect, useMemo, useState } from 'react'
import { apiGet, apiPost, apiPut } from '../api/client.js'
import Icon from '../components/Icon.jsx'
import Modal from '../components/Modal.jsx'
import StatCard from '../components/StatCard.jsx'
import { initials } from '../utils.js'

export default function AdminPage() {
  const [summary, setSummary] = useState(null)
  const [users, setUsers] = useState([])
  const [types, setTypes] = useState([])
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [typeModal, setTypeModal] = useState(false)
  const [allocationModal, setAllocationModal] = useState(false)
  const [editingType, setEditingType] = useState(null)
  const [typeForm, setTypeForm] = useState(emptyType())
  const [allocation, setAllocation] = useState({ userId: '', leaveTypeId: '', year: new Date().getFullYear(), allocated: 0 })
  const [saving, setSaving] = useState(false)

  const load = async () => {
    try {
      const [s, u, t] = await Promise.all([
        apiGet('/admin/summary'),
        apiGet('/admin/users'),
        apiGet('/leave-types?includeInactive=true'),
      ])
      setSummary(s); setUsers(u); setTypes(t)
      if (u.length && t.length) {
        setAllocation((a) => ({ ...a, userId: a.userId || u[0].id, leaveTypeId: a.leaveTypeId || String(t[0].id), allocated: a.allocated || t[0].defaultDays }))
      }
    } catch (e) {
      setError(e.message)
    }
  }

  useEffect(() => { load() }, [])

  const openNewType = () => { setEditingType(null); setTypeForm(emptyType()); setTypeModal(true) }
  const openEditType = (type) => { setEditingType(type); setTypeForm({ name: type.name, code: type.code, color: type.color, defaultDays: type.defaultDays, isPaid: type.isPaid, isActive: type.isActive }); setTypeModal(true) }

  const saveType = async () => {
    setSaving(true); setError('')
    try {
      if (editingType) await apiPut(`/admin/leave-types/${editingType.id}`, { ...typeForm, defaultDays: Number(typeForm.defaultDays) })
      else await apiPost('/admin/leave-types', { ...typeForm, defaultDays: Number(typeForm.defaultDays) })
      setTypeModal(false); setMessage(editingType ? 'Leave type updated.' : 'Leave type created and balances initialized.'); await load()
    } catch (e) { setError(e.message) } finally { setSaving(false) }
  }

  const saveAllocation = async () => {
    setSaving(true); setError('')
    try {
      await apiPost('/admin/allocate-balance', {
        userId: allocation.userId,
        leaveTypeId: Number(allocation.leaveTypeId),
        year: Number(allocation.year),
        allocated: Number(allocation.allocated),
      })
      setAllocationModal(false); setMessage('Employee leave allocation updated.')
    } catch (e) { setError(e.message) } finally { setSaving(false) }
  }

  const selectedType = useMemo(() => types.find((x) => x.id === Number(allocation.leaveTypeId)), [types, allocation.leaveTypeId])

  return (
    <div className="stack-lg">
      {message && <div className="alert success"><Icon name="check" size={18} />{message}<button onClick={() => setMessage('')}>×</button></div>}
      {error && <div className="alert error"><Icon name="alert" size={18} />{error}</div>}

      {summary && <section className="stats-grid admin-stats">
        <StatCard label="Active users" value={summary.employees} helper="Across the organization" icon="users" tone="blue" />
        <StatCard label="Pending requests" value={summary.pendingRequests} helper="Awaiting manager review" icon="clock" tone="amber" />
        <StatCard label="Approved this year" value={summary.approvedThisYear} helper="Organization-wide" icon="check" tone="green" />
        <StatCard label="Active leave types" value={summary.leaveTypes} helper="Current policy catalog" icon="calendar" tone="indigo" />
      </section>}

      <section className="content-grid admin-grid">
        <article className="panel">
          <div className="panel-header">
            <div><h2>Leave policy catalog</h2><p>Manage organization leave types and default yearly allocations.</p></div>
            <button className="primary-button" onClick={openNewType}><Icon name="plus" size={17} /> Add type</button>
          </div>
          <div className="policy-list">
            {types.map((type) => (
              <button key={type.id} className="policy-row" onClick={() => openEditType(type)}>
                <span className="policy-color" style={{ background: type.color }} />
                <span className="policy-main"><strong>{type.name}</strong><small>{type.code} · {type.isPaid ? 'Paid' : 'Unpaid'}</small></span>
                <span className="policy-days"><strong>{type.defaultDays}</strong><small>default days</small></span>
                <span className={`policy-status ${type.isActive ? 'on' : 'off'}`}>{type.isActive ? 'Active' : 'Inactive'}</span>
                <Icon name="arrow" size={16} />
              </button>
            ))}
          </div>
        </article>

        <article className="panel compact-panel">
          <div className="panel-header"><div><h2>Balance allocation</h2><p>Adjust a yearly entitlement.</p></div></div>
          <div className="admin-action-card">
            <div className="admin-action-icon"><Icon name="calendar" /></div>
            <div><strong>Allocate leave balance</strong><p>Set or update an employee’s yearly days for a leave type.</p></div>
            <button className="secondary-button" onClick={() => setAllocationModal(true)}>Manage</button>
          </div>
        </article>
      </section>

      <article className="panel flush-panel">
        <div className="panel-header padded-header"><div><h2>Employee directory</h2><p>Seeded users and their organizational roles.</p></div><span className="count-pill">{users.length} users</span></div>
        <div className="table-wrap">
          <table>
            <thead><tr><th>Employee</th><th>Department</th><th>Job title</th><th>Manager</th><th>Role</th><th>Status</th></tr></thead>
            <tbody>{users.map((user) => <tr key={user.id}>
              <td><div className="person-cell"><div className="avatar small-avatar">{initials(user.fullName)}</div><div><strong>{user.fullName}</strong><small>{user.email}</small></div></div></td>
              <td>{user.department}</td><td>{user.jobTitle}</td><td>{user.managerName || '—'}</td>
              <td><span className="role-pill table-role">{user.roles.join(', ')}</span></td>
              <td><span className={`policy-status ${user.isActive ? 'on' : 'off'}`}>{user.isActive ? 'Active' : 'Inactive'}</span></td>
            </tr>)}</tbody>
          </table>
        </div>
      </article>

      <Modal open={typeModal} title={editingType ? 'Edit leave type' : 'Create leave type'} onClose={() => setTypeModal(false)}>
        <div className="leave-form modal-form">
          <label className="field"><span>Name</span><input value={typeForm.name} onChange={(e) => setTypeForm({ ...typeForm, name: e.target.value })} placeholder="e.g. Compensatory Off" /></label>
          <label className="field"><span>Code</span><input value={typeForm.code} onChange={(e) => setTypeForm({ ...typeForm, code: e.target.value.toUpperCase() })} maxLength="20" placeholder="CO" /></label>
          <label className="field"><span>Default days</span><input type="number" min="0" max="365" step="0.5" value={typeForm.defaultDays} onChange={(e) => setTypeForm({ ...typeForm, defaultDays: e.target.value })} /></label>
          <label className="field"><span>Accent color</span><div className="color-input"><input type="color" value={typeForm.color} onChange={(e) => setTypeForm({ ...typeForm, color: e.target.value })} /><input value={typeForm.color} onChange={(e) => setTypeForm({ ...typeForm, color: e.target.value })} /></div></label>
          <label className="check-field"><input type="checkbox" checked={typeForm.isPaid} onChange={(e) => setTypeForm({ ...typeForm, isPaid: e.target.checked })} /><span><strong>Paid leave</strong><small>Time off is treated as paid.</small></span></label>
          <label className="check-field"><input type="checkbox" checked={typeForm.isActive} onChange={(e) => setTypeForm({ ...typeForm, isActive: e.target.checked })} /><span><strong>Active</strong><small>Employees can choose this type for new requests.</small></span></label>
          <div className="modal-actions full-field"><button className="secondary-button" onClick={() => setTypeModal(false)}>Cancel</button><button className="primary-button" disabled={saving || !typeForm.name.trim() || !typeForm.code.trim()} onClick={saveType}>{saving ? 'Saving...' : 'Save leave type'}</button></div>
        </div>
      </Modal>

      <Modal open={allocationModal} title="Allocate leave balance" onClose={() => setAllocationModal(false)}>
        <div className="leave-form modal-form">
          <label className="field full-field"><span>Employee</span><select value={allocation.userId} onChange={(e) => setAllocation({ ...allocation, userId: e.target.value })}>{users.map((user) => <option key={user.id} value={user.id}>{user.fullName} — {user.department}</option>)}</select></label>
          <label className="field"><span>Leave type</span><select value={allocation.leaveTypeId} onChange={(e) => { const id = e.target.value; const type = types.find((x) => x.id === Number(id)); setAllocation({ ...allocation, leaveTypeId: id, allocated: type?.defaultDays ?? allocation.allocated }) }}>{types.map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label>
          <label className="field"><span>Year</span><input type="number" min="2000" max="2200" value={allocation.year} onChange={(e) => setAllocation({ ...allocation, year: e.target.value })} /></label>
          <label className="field full-field"><span>Allocated days</span><input type="number" min="0" max="365" step="0.5" value={allocation.allocated} onChange={(e) => setAllocation({ ...allocation, allocated: e.target.value })} /><small>Policy default: {selectedType?.defaultDays ?? '—'} days</small></label>
          <div className="modal-actions full-field"><button className="secondary-button" onClick={() => setAllocationModal(false)}>Cancel</button><button className="primary-button" disabled={saving} onClick={saveAllocation}>{saving ? 'Saving...' : 'Update allocation'}</button></div>
        </div>
      </Modal>
    </div>
  )
}

function emptyType() {
  return { name: '', code: '', color: '#4f46e5', defaultDays: 10, isPaid: true, isActive: true }
}
