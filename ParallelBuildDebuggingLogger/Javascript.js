function filter() {
    var filter, listitems, i;
    filter = new RegExp(document.getElementById("searchbox").value, "i");
    listitems = document.getElementById("projects").getElementsByTagName("li");
    for (i = 0; i < listitems.length; i++) {
        if (listitems[i].getElementsByClassName("projectdescription")[0].textContent.match(filter)) {
            listitems[i].style.display = "";
        } else {
            listitems[i].style.display = "none";
        }
    }
}
