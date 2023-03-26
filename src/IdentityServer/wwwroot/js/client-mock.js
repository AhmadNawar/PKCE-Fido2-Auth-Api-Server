$(function () {
    async function generateCodeChallenge(codeVerifier) {
        var digest = await crypto.subtle.digest("SHA-256",
            new TextEncoder().encode(codeVerifier));

        return btoa(String.fromCharCode(...new Uint8Array(digest)))
            .replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_')
    }

    $("#weather-button").on("click", (e) => {
        e.preventDefault();
        // Generate a random array of bytes
        var array = new Uint8Array(128);
        window.crypto.getRandomValues(array);

        // Convert the bytes to a string
        var randomString = btoa(String.fromCharCode.apply(null, array));

        // Trim the string to the desired length
        const codeVerifier = randomString.substring(0, Math.floor(Math.random() * (128 - 43)) + 43);

        // Save the string to a session
        window.sessionStorage.setItem("code_verifier", codeVerifier);

        const clientId = 'pkce_client';

        const grantType = 'authorization_code';

        const redirectUri = 'https://localhost:5001/callback';

        const authorizeEndpoint = 'https://localhost:5001/connect/authorize';

        generateCodeChallenge(codeVerifier).then((codeChallenge) => {
            var args = new URLSearchParams({
                response_type: "code",
                client_id: clientId,
                code_challenge_method: "S256",
                code_challenge: codeChallenge,
                redirect_uri: redirectUri,
                scope: 'api1',
                grant_type: grantType
            });
            //const url = `https://localhost:5001/connect/authorize?client_id=${clientId}&grant_type=${grantType}&response_type=code&redirect_uri=${redirectUri}&scope=api1&code_challenge_method=S256&code_challenge=${codeChallange}`
            window.location = authorizeEndpoint + "?" + args;
        });
    });
});