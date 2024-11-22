function toggleAutocompletePopup(visible, suggestions) {
    console.log('Toggling autocomplete...')
    const magicBar = document.getElementById('magic-bar');
    let popup = document.getElementById('autocomplete-popup');

    if (!popup) {
        popup = document.createElement('div');
        popup.id = 'autocomplete-popup';
        popup.style.position = 'absolute';
        popup.style.border = '1px solid #ccc';
        popup.style.backgroundColor = '#fff';
        popup.style.zIndex = '1000';
        popup.style.overflowY = 'auto';
        document.body.appendChild(popup);
    }

    if (visible) {
        const rect = magicBar.getBoundingClientRect();
        const maxHeight = window.innerHeight / 2;
        popup.style.left = `${rect.left}px`;
        popup.style.bottom = `${window.innerHeight - rect.top}px`;
        popup.style.maxHeight = `${maxHeight}px`;
        popup.style.display = 'block';
        popup.style.margin = '5px';
        popup.style.marginBottom = '-5px';
        popup.style.borderRadius = '5px';

        popup.innerHTML = '';
        suggestions.forEach(suggestion => {
            const item = document.createElement('div');
            item.textContent = suggestion;
            item.style.padding = '5px';
            item.style.cursor = 'pointer';
            item.addEventListener('click', () => {
                magicBar.value = suggestion;
                popup.style.display = 'none';
            });
            popup.appendChild(item);
        });
    } else {
        popup.style.display = 'none';
    }
}
