function toggleAutocompletePopup(visible, suggestions) {
    const magicBar = document.getElementById('magic-bar');
    let popup = document.getElementById('autocomplete-popup');
    console.log('toggling autocomplete...')

    if (!popup) {
        popup = document.createElement('div');
        popup.id = 'autocomplete-popup';
        popup.style.position = 'absolute';
        popup.style.border = '1px solid #ccc';
        popup.style.backgroundColor = '#fff';
        popup.style.zIndex = '1000';
        document.body.appendChild(popup);
    }

    if (visible) {
        const rect = magicBar.getBoundingClientRect();
        popup.style.left = `${rect.left}px`;
        popup.style.top = `${rect.top - popup.offsetHeight}px`;
        popup.style.display = 'block';

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
