(() => {
	let runWfBtns = document.getElementsByClassName('run-wf-btn');
	for (var i = 0; i < runWfBtns.length; i++) {
		let btn = runWfBtns[i];
		btn.onclick = function () {
			let wfId = this.getAttribute('data-id');
			let xhttp = new XMLHttpRequest();
			xhttp.onreadystatechange = function () {				
				if (this.readyState == 4) {
					let res = '';
					if (this.status == 200) {
						res = 'Success';
					} else if (this.status >= 400) {
						res = 'Error';
					}
					let toastNotif = Toastify({
						text: `${res} running workflow with id: ${wfId}`,
						duration: 5000
					});
					toastNotif.showToast();
				}				
			};
			xhttp.open('POST', 'home/runworkflow', true);
			xhttp.setRequestHeader("content-type", "application/json");
			xhttp.send(wfId);
		};
	}
})();