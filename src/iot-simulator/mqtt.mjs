// Minimal MQTT 3.1.1 publisher (QoS 0) over TCP, so the simulator needs no npm dependencies.
// Only CONNECT, PUBLISH, PINGREQ and DISCONNECT: the cabin gateways only publish telemetry.
import net from 'node:net'

function encodeLength(length) {
  const bytes = []
  do {
    let byte = length % 128
    length = Math.floor(length / 128)
    if (length > 0) byte |= 0x80
    bytes.push(byte)
  } while (length > 0)
  return Buffer.from(bytes)
}

const utf8 = (text) => {
  const body = Buffer.from(text, 'utf8')
  const size = Buffer.alloc(2)
  size.writeUInt16BE(body.length)
  return Buffer.concat([size, body])
}

const packet = (header, body) => Buffer.concat([Buffer.from([header]), encodeLength(body.length), body])

export class MqttPublisher {
  constructor({ host, port = 1883, clientId, keepAliveSeconds = 30, log = console }) {
    Object.assign(this, { host, port, clientId, keepAliveSeconds, log })
    this.connected = false
    this.published = 0
    this.socket = null
  }

  start() {
    if (!this.host) return
    const socket = net.createConnection({ host: this.host, port: this.port })
    this.socket = socket
    socket.on('connect', () => {
      const keepAlive = Buffer.alloc(2)
      keepAlive.writeUInt16BE(this.keepAliveSeconds)
      // Protocol "MQTT" level 4, clean session.
      const body = Buffer.concat([utf8('MQTT'), Buffer.from([0x04, 0x02]), keepAlive, utf8(this.clientId)])
      socket.write(packet(0x10, body))
    })
    socket.on('data', (data) => {
      // CONNACK: 0x20 0x02 <flags> <return code>
      if (data[0] === 0x20 && data[3] === 0x00 && !this.connected) {
        this.connected = true
        this.log.info(`MQTT conectado a ${this.host}:${this.port}`)
        this.ping = setInterval(() => socket.write(Buffer.from([0xc0, 0x00])), (this.keepAliveSeconds * 1000) / 2)
      }
    })
    const retry = () => {
      if (this.ping) clearInterval(this.ping)
      if (this.connected) this.log.warn('MQTT desconectado, reintentando')
      this.connected = false
      setTimeout(() => this.start(), 5000)
    }
    socket.on('error', () => {})
    socket.on('close', retry)
  }

  publish(topic, message) {
    if (!this.connected) return false
    const payload = Buffer.from(typeof message === 'string' ? message : JSON.stringify(message))
    this.socket.write(packet(0x30, Buffer.concat([utf8(topic), payload])))
    this.published++
    return true
  }
}
