<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '../api'
import { isWebAuthnSupported, createPasskey } from '../passkey'

const router = useRouter()
const user = ref(null)
const passkeys = ref([])
const error = ref('')
const busy = ref(false)
const newPasskeyName = ref('')
const webAuthnSupported = isWebAuthnSupported()

onMounted(async () => {
  try {
    user.value = await authApi.me()
    passkeys.value = await authApi.listPasskeys()
  } catch {
    router.push('/')
  }
})

async function addPasskey() {
  error.value = ''
  busy.value = true
  try {
    const name = newPasskeyName.value.trim() || undefined
    await createPasskey(name)
    passkeys.value = await authApi.listPasskeys()
    user.value = await authApi.me()
    newPasskeyName.value = ''
  } catch (err) {
    error.value = err.message || 'Passkey registration failed or was cancelled.'
  } finally {
    busy.value = false
  }
}

async function removePasskey(id) {
  error.value = ''
  busy.value = true
  try {
    await authApi.deletePasskey(id)
    passkeys.value = await authApi.listPasskeys()
    user.value = await authApi.me()
  } catch (err) {
    error.value = err.message || 'Could not delete the passkey.'
  } finally {
    busy.value = false
  }
}

async function logout() {
  await authApi.logout()
  router.push('/')
}
</script>

<template>
  <template v-if="user">
    <h1>Dashboard</h1>
    <p>Signed in as <strong>{{ user.email }}</strong></p>

    <h2>Passkeys ({{ passkeys.length }})</h2>
    <ul v-if="passkeys.length">
      <li v-for="key in passkeys" :key="key.id">
        {{ key.name }}
        <button class="secondary" type="button" @click="removePasskey(key.id)">Delete</button>
      </li>
    </ul>
    <p v-else>No passkeys registered yet.</p>

    <form v-if="webAuthnSupported" @submit.prevent="addPasskey">
      <label for="name">Passkey name (optional)</label>
      <input id="name" v-model="newPasskeyName" type="text" autocomplete="off" />
      <button type="submit" :disabled="busy">Add a passkey</button>
    </form>
    <p v-else>This browser does not support passkeys.</p>

    <button class="secondary" type="button" @click="logout">Sign out</button>
    <p class="error">{{ error }}</p>
  </template>
</template>