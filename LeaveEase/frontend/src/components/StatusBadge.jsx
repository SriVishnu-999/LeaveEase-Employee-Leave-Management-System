export default function StatusBadge({ status }) {
  const value = status || 'Pending'
  return <span className={`status-badge status-${value.toLowerCase()}`}>{value}</span>
}
