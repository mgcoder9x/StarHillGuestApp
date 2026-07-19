import { ref } from 'vue';

export const toastMessage = ref('');
export const toastVisible = ref(false);
let toastTimeout: any = null;

export function showToast(msg: string) {
  toastMessage.value = msg;
  toastVisible.value = true;
  if (toastTimeout) clearTimeout(toastTimeout);
  toastTimeout = setTimeout(() => {
    toastVisible.value = false;
  }, 3000);
}
