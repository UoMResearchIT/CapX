// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

function toggleAutocompletePopup(visible, suggestions, razorComponentReference) {
    console.log('Toggling autocomplete...')
    const magicBar = document.getElementById('magic-bar');
    let popup = document.getElementById('autocomplete-popup');

    // If we are showing the popup
    if (visible) {

        // If it doesn't already exist then create the element
        if (!popup) {
            popup = document.createElement('div');
            popup.id = 'autocomplete-popup';
            popup.style.position = 'absolute';
            popup.style.border = 'var(--rz-card-border)';
            popup.style.backgroundColor = 'var(--rz-card-background-color)';
            popup.style.zIndex = '1000';
            popup.style.overflowY = 'auto';
            document.body.appendChild(popup);
        }

        // Style and position
        const rect = magicBar.getBoundingClientRect();
        const maxHeight = window.innerHeight / 2;
        popup.style.left = `${rect.left}px`;
        popup.style.bottom = `${window.innerHeight - rect.top}px`;
        popup.style.maxHeight = `${maxHeight}px`;
        popup.style.width = `${rect.width}px`;
        popup.style.display = 'block';
        popup.style.margin = '5px';
        popup.style.marginBottom = '-5px';
        popup.style.borderRadius = '5px';

        // Add the list items
        popup.innerHTML = '';
        suggestions.forEach((suggestion, index) => {
            const item = document.createElement('div');
            item.textContent = suggestion;
            item.style.padding = '5px';
            item.style.cursor = 'pointer';
            item.addEventListener('click', () => {
                if (!popup) return;
                console.log(`Selected: ${suggestion}`);
                razorComponentReference.invokeMethodAsync('OnItemSelected', suggestion);
            });
            popup.appendChild(item);
        });
        
    } else {
        destroyAutocompletePopup();
    }
}

function destroyAutocompletePopup() {
    console.log('Hiding autocomplete...')
    let popup = document.getElementById('autocomplete-popup');
    if (popup) {
        document.body.removeChild(popup);
    }
}
