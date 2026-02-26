window.dslkitSourceEditor = {
    revealSelection(element, start, end) {
        if (!element || typeof element.focus !== "function") {
            return;
        }

        const safeStart = Math.max(0, Number(start) || 0);
        const safeEnd = Math.max(safeStart, Number(end) || safeStart);

        element.focus();
        if (typeof element.setSelectionRange === "function") {
            element.setSelectionRange(safeStart, safeEnd);
        }

        const lineHeight = parseFloat(getComputedStyle(element).lineHeight) || 20;
        const beforeSelection = element.value.slice(0, safeStart);
        const lineIndex = beforeSelection.split("\n").length - 1;
        const targetScrollTop = Math.max(0, lineIndex * lineHeight - element.clientHeight / 2);
        element.scrollTop = targetScrollTop;
    }
};
