import { create } from 'zustand'

interface RagChatUiState {
  isPopupOpen: boolean
  setPopupOpen: (open: boolean) => void
  openPopup: () => void
  closePopup: () => void
}

export const useRagChatUiStore = create<RagChatUiState>()((set) => ({
  isPopupOpen: false,

  setPopupOpen: (open) => set({ isPopupOpen: open }),

  openPopup: () => set({ isPopupOpen: true }),

  closePopup: () => set({ isPopupOpen: false }),
}))
