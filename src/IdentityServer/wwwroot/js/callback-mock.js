$(function () {
    function showWeatherInfo(data) {
        const tableBody = document.querySelector("#weather-data tbody");

        data.forEach(item => {
            const row = document.createElement("tr");

            const dateCell = document.createElement("td");
            dateCell.textContent = new Date(item.date).toLocaleDateString();
            row.appendChild(dateCell);

            const tempCCell = document.createElement("td");
            tempCCell.textContent = item.temperatureC;
            row.appendChild(tempCCell);

            const tempFCell = document.createElement("td");
            tempFCell.textContent = item.temperatureF;
            row.appendChild(tempFCell);

            const summaryCell = document.createElement("td");
            summaryCell.textContent = item.summary;
            row.appendChild(summaryCell);

            tableBody.appendChild(row);
        });
    }

    var headers = new Headers();
    headers.append("Content-Type", "application/x-www-form-urlencoded");

    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);

    var tokenParams = new URLSearchParams();
    tokenParams.append("grant_type", "authorization_code");
    tokenParams.append("code_verifier", window.sessionStorage.getItem("code_verifier"));
    tokenParams.append("code", urlParams.get('code'));
    tokenParams.append("client_id", "pkce_client");
    tokenParams.append("redirect_uri", "https://localhost:5001/callback");
    tokenParams.append("scope", "api1");

    var requestOptions = {
        method: 'POST',
        headers: headers,
        body: tokenParams,
        redirect: 'follow'
    };

    fetch("https://localhost:5001/connect/token", requestOptions)
        .then(response => response.json())
        .then(result => {
            // Got the access token correctly
            if (result.access_token) {
                var weatherHeaders = new Headers();
                weatherHeaders.append("Authorization", `Bearer ${result.access_token}`);

                var requestOptions = {
                    method: 'GET',
                    headers: weatherHeaders
                };

                fetch("https://localhost:44347/WeatherForecast", requestOptions)
                    .then(response => response.json())
                    .then(result => showWeatherInfo(result))
                    .catch(error => console.log('error', error));
            }
        })
        .catch(error => console.log('error', error));
});