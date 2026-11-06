const STORAGE_KEY = "climato.theme";
const MODES = ["light", "dark"];

const readSavedMode = () => {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
};

const systemPrefersDark = () =>
  window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;

export const getInitialMode = () => {
  const saved = readSavedMode();

  if (MODES.includes(saved)) {
    return saved;
  }

  return systemPrefersDark() ? "dark" : "light";
};

export const saveMode = (mode) => {
  try {
    localStorage.setItem(STORAGE_KEY, mode);
  } catch {
    // armazenamento bloqueado (ex.: modo privado): a escolha vale só nesta aba
  }
};
