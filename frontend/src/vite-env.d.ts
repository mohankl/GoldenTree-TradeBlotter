/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the .NET API. Unset in dev (relative paths via the Vite proxy). */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
