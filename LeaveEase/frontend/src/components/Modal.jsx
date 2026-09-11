import { useEffect } from 'react'
import Icon from './Icon.jsx'

export default function Modal({ open, title, children, onClose, width = '560px' }) {
  useEffect(() => {
    if (!open) return
    const onKeyDown = (event) => event.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  if (!open) return null
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <section className="modal-card" style={{ maxWidth: width }} onMouseDown={(e) => e.stopPropagation()}>
        <header className="modal-header">
          <h2>{title}</h2>
          <button className="icon-button" onClick={onClose} aria-label="Close"><Icon name="close" /></button>
        </header>
        <div className="modal-body">{children}</div>
      </section>
    </div>
  )
}
