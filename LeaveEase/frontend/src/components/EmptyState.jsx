import Icon from './Icon.jsx'

export default function EmptyState({ title, description, icon = 'calendar', action }) {
  return (
    <div className="empty-state">
      <div className="empty-icon"><Icon name={icon} size={25} /></div>
      <h3>{title}</h3>
      <p>{description}</p>
      {action}
    </div>
  )
}
