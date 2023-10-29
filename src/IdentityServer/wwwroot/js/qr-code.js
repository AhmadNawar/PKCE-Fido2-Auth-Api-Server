$(function () {
	const baseUrl = 'https://localhost:5001/api/qRCodeAuthentication';
	var pollInterval;

	function pollAuthenticationStatus(username, sessionId) {
		setInterval(() => {
			fetch(`${baseUrl}/get-session-status?username=${username}&sessionId=${sessionId}`)
				.then(response => response.json())
				.then(data => {
					if (data.authenticated === true) {
						console.log('User is authenticated!');
						// Perform further actions or update UI as needed
						clearInterval(pollInterval);
						window.location.reload();
					}
				})
				.catch(error => {
					console.error('Error occurred during authentication status polling:', error);
				});
		}, 1000); // Polling interval: 1 second
	}

	$(".qr-button").on("click", function(e) {
		e.preventDefault();
		const actionType = $(this).data("action-type");
		
		let requestOptions = {
			method: 'GET',
		};

		let username = $("#username").val();

		fetch(`${baseUrl}/generate-qr-challange/${username}`, requestOptions)
			.then(response => response.json())
			.then(data => {
				const code = data.code;
				const sessionId = data.sessionId;
				const url = `${window.location.host}/?c=${code}&u=${username}&s=${sessionId}?a=${actionType}`;

				var qrcode = new QRCode(document.getElementById("qrcode"), {
					text: url,
					width: 128,
					height: 128,
					colorDark: "#000000",
					colorLight: "#ffffff"
				});

				// Start polling authentication status
				//pollInterval = pollAuthenticationStatus(username, sessionId);
			}).catch(error => {
				console.warn(error);
            });
	});
});