async function findClosest() {
    const latitude = document.getElementById("latitude").value;
    const longitude = document.getElementById("longitude").value;

    const result = await fetch(`/Home/ClosestCentreline?latitude=${latitude}&longitude=${longitude}`).then(response => response.text());
    console.log(result);
    document.getElementById("result").textContent = result;
}