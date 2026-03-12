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
    },

    revealSelectionById(elementId, start, end) {
        if (typeof elementId !== "string" || !elementId) {
            return;
        }

        const element = document.getElementById(elementId);
        this.revealSelection(element, start, end);
    }
};

window.dslkitSqlEditor = {
    autoSizeTextAreaById(elementId, minHeight) {
        if (typeof elementId !== "string" || !elementId) {
            return;
        }

        const element = document.getElementById(elementId);
        if (!element || element.tagName !== "TEXTAREA") {
            return;
        }

        const safeMinHeight = Math.max(0, Number(minHeight) || 0);

        element.style.overflowY = "hidden";
        element.style.height = "auto";

        const nextHeight = Math.max(safeMinHeight, element.scrollHeight);
        element.style.height = `${nextHeight}px`;
    },

    autoSizeEditors(sourceInputId, formattedOutputId, minHeight) {
        this.autoSizeTextAreaById(sourceInputId, minHeight);
        this.autoSizeTextAreaById(formattedOutputId, minHeight);
    },

    positionHelpPopover(triggerElement, popoverElement) {
        if (!triggerElement || !popoverElement) {
            return;
        }

        const triggerRect = triggerElement.getBoundingClientRect();
        if (!triggerRect.width && !triggerRect.height) {
            return;
        }

        const viewportWidth = window.innerWidth || document.documentElement.clientWidth || 0;
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight || 0;
        const gap = 8;
        const margin = 12;

        const popoverWidth = popoverElement.offsetWidth;
        const popoverHeight = popoverElement.offsetHeight;
        if (!popoverWidth || !popoverHeight) {
            return;
        }

        const canPlaceRight = triggerRect.right + gap + popoverWidth <= viewportWidth - margin;
        const canPlaceLeft = triggerRect.left - gap - popoverWidth >= margin;
        const shouldPlaceBelow = viewportWidth < 992 || (!canPlaceRight && !canPlaceLeft);

        let left;
        let top;

        if (shouldPlaceBelow) {
            left = triggerRect.left + triggerRect.width / 2 - popoverWidth / 2;
            left = Math.max(margin, Math.min(left, viewportWidth - popoverWidth - margin));

            const belowTop = triggerRect.bottom + gap;
            const aboveTop = triggerRect.top - popoverHeight - gap;
            top = belowTop + popoverHeight <= viewportHeight - margin || aboveTop < margin
                ? belowTop
                : aboveTop;
        } else if (canPlaceRight) {
            left = triggerRect.right + gap;
            top = triggerRect.top + triggerRect.height / 2 - popoverHeight / 2;
        } else {
            left = triggerRect.left - popoverWidth - gap;
            top = triggerRect.top + triggerRect.height / 2 - popoverHeight / 2;
        }

        top = Math.max(margin, Math.min(top, viewportHeight - popoverHeight - margin));

        popoverElement.style.left = `${Math.round(left)}px`;
        popoverElement.style.top = `${Math.round(top)}px`;
    }
};
