function filter() {
    var filter, listitems, i, show;
    filter = new RegExp(document.getElementById("searchbox").value, "i");
    showreenter = document.getElementById("showreenter").checked;
    listitems = document.getElementById("projects").getElementsByTagName("li");
    for (i = 0; i < listitems.length; i++) {
        show = true;

        if (listitems[i].className === "reentered") {
            show = showreenter;
        }

        if (!listitems[i].getElementsByClassName("projectdescription")[0].textContent.match(filter)) {
            show = false;
        }

        if (show) {
            listitems[i].style.display = "";
        } else {
            listitems[i].style.display = "none";
        }
    }
}
