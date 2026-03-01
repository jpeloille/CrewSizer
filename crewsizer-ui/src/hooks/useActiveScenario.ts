import { useState, useCallback } from 'react';

export function useActiveScenario() {
  const [activeScenarioId, setActiveScenarioIdState] = useState<string | null>(
    () => localStorage.getItem('activeScenarioId')
  );

  const setActiveScenarioId = useCallback((id: string | null) => {
    if (id) {
      localStorage.setItem('activeScenarioId', id);
    } else {
      localStorage.removeItem('activeScenarioId');
    }
    setActiveScenarioIdState(id);
  }, []);

  return { activeScenarioId, setActiveScenarioId };
}
