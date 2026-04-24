// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
let timer;
const input = document.getElementById("doctorSearch");
const resultsBox = document.getElementById("doctorSearchResults");

if (input) {
    input.addEventListener("keyup", function () {
        clearTimeout(timer);
        const term = this.value.trim();

        if (term.length < 2) {
            resultsBox.style.display = "none";
            return;
        }

        timer = setTimeout(() => {
            fetch(`/Doctors/SearchDoctors?term=${encodeURIComponent(term)}`)
                .then(res => res.json())
                .then(data => {
                    resultsBox.innerHTML = "";

                    if (data.length === 0) {
                        resultsBox.innerHTML =
                            `<div class="list-group-item text-muted">No doctors found</div>`;
                    } else {
                        data.forEach(d => {
                            resultsBox.innerHTML += `
                                <a href="/Doctors/Details/${d.id}"
                                   class="list-group-item list-group-item-action">
                                    <strong>Dr. ${d.name}</strong><br/>
                                    <small class="text-muted">${d.specialization}</small>
                                </a>`;
                        });
                    }

                    resultsBox.style.display = "block";
                });
        }, 300);
    });
}
