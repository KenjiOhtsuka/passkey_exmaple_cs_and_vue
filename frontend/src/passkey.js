import { startRegistration, startAuthentication, browserSupportsWebAuthn } from '@simplewebauthn/browser'
import { authApi } from './api'

export function isWebAuthnSupported() {
  return browserSupportsWebAuthn()
}

export async function createPasskey(name) {
  const options = await authApi.passkeyCreateOptions()
  const attestation = await startRegistration({ optionsJSON: options })
  return authApi.passkeyRegister(JSON.stringify(attestation), name)
}

export async function loginWithPasskey(username) {
  const options = await authApi.passkeyLoginOptions(username)
  const assertion = await startAuthentication({ optionsJSON: options })
  return authApi.passkeyLogin(JSON.stringify(assertion))
}