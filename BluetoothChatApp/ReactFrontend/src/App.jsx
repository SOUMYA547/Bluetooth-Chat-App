import { useEffect, useState } from 'react'

export default function App(){
  const [chat, setChat] = useState('')
  const [text, setText] = useState('')

  useEffect(() => {
    const handler = (e) => {
      try {
        const { type, payload } = JSON.parse(e.data)
        if (type === 'incoming') setChat(x => x + `\nPeer: ${payload}`)
      } catch {}
    }
    window.chrome?.webview?.addEventListener('message', handler)
    return () => window.chrome?.webview?.removeEventListener('message', handler)
  }, [])

  const send = () => {
    window.chrome?.webview?.postMessage(JSON.stringify({ type: 'send', text }))
    setChat(x => x + `\nMe: ${text}`)
    setText('')
  }

  return (
    <div style={{padding:16, fontFamily:'sans-serif'}}>
      <h2>Bluetooth Chat – React UI</h2>
      <div style={{border:'1px solid #ccc', borderRadius:12, padding:12, marginTop:8}}>
        <div style={{height:200, overflow:'auto', whiteSpace:'pre-wrap'}}>{chat || 'Chat log…'}</div>
        <div style={{display:'flex', gap:8, marginTop:8}}>
          <input placeholder="Type message" style={{flex:1}} value={text} onChange={e=>setText(e.target.value)} />
          <button onClick={send}>Send</button>
        </div>
      </div>
    </div>
  )
}
