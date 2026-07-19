import { ref } from 'vue';

export const ruleConfirmed = ref(sessionStorage.getItem('starhill_rule_confirmed') === 'true');

export function confirmRules() {
  ruleConfirmed.value = true;
  sessionStorage.setItem('starhill_rule_confirmed', 'true');
}

export function resetRules() {
  ruleConfirmed.value = false;
  sessionStorage.removeItem('starhill_rule_confirmed');
}
