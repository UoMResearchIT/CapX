// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

// Function to set a CSS variable at runtime by injecting into DOM
window.setCssVariable = (name, value) => {

    // Apply to HTML to begin with
    document.documentElement.style.setProperty(name, value);

    // Radzen has specific selectors for themes so try here too
    document.querySelectorAll('.rz-material, .rz-material-dark')
        .forEach(el => el.style.setProperty(name, value));

};