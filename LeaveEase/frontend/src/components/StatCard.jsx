import Icon from './Icon.jsx'

export default function StatCard({ label, value, helper, icon = 'calendar', tone = 'indigo' }) {
  return (
    <article className="stat-card">
      <div className={`stat-icon tone-${tone}`}><Icon name={icon} size={20} /></div>
      <div>
        <p className="stat-label">{label}</p>
        <strong className="stat-value">{value}</strong>
        {helper && <p className="stat-helper">{helper}</p>}
      </div>
    </article>
  )
}
