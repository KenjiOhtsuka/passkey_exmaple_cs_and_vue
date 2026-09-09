<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '../api'
import { isWebAuthnSupported, loginWithPasskey } from '../passkey'

const router = useRouter()
const email = ref('')
const password = ref('')
const error = ref('')
const busy = ref(false)
const webAuthnSupported = isWebAuthnSupported()

async function submitPassword() {
  error.value = ''
  busy.value = true
  try {
    await authApi.login(email.value, password.value)
    router.push('/dashboard')
  } catch (err) {
    error.value = err.message || 'Login failed.'
  } finally {
    busy.value = false
  }
}

async function submitPasskey() {
  error.value = ''
  busy.value = true
  try {
    await loginWithPasskey(email.value)
    router.push('/dashboard')
  } catch (err) {
    error.value = err.message || 'Passkey sign-in failed or was cancelled.'
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <h1>Sign in</h1>
  <form @submit.prevent="submitPassword">
    <label for="email">Email</label>
    <input id="email" v-model="email" type="email" autocomplete="username" required />

    <label for="password">Password</label>
    <input id="password" v-model="password" type="password" autocomplete="current-password" />

    <button type="submit" :disabled="busy">Sign in</button>
  </form>

  <p v-if="webAuthnSupported">
    <button class="secondary" type="button" :disabled="busy" @click="submitPasskey">
      Sign in with a passkey
    </button>
  </p>
  <p v-else>Passkeys are not supported in this browser.</p>

  <p class="error">{{ error }}</p>
</template>