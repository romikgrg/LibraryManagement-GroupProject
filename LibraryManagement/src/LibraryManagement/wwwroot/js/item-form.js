// Shows only the fields that belong to the item type currently selected.
// Every block stays in the DOM, so a validation failure never loses input.
(function () {
    "use strict";

    var selector = document.getElementById("itemKind");
    var blocks = Array.prototype.slice.call(document.querySelectorAll(".kind-fields"));
    if (blocks.length === 0) {
        return;
    }

    function currentKind() {
        if (selector) {
            return selector.options[selector.selectedIndex].text.trim();
        }

        // On the edit screen the type is fixed, so read it from the hidden input.
        var fixed = document.querySelector("input[name='Kind']");
        return fixed ? fixed.value.trim() : "";
    }

    function show() {
        var kind = currentKind();
        blocks.forEach(function (block) {
            block.hidden = block.getAttribute("data-kind") !== kind;
        });
    }

    if (selector) {
        selector.addEventListener("change", show);
    }

    show();
})();
