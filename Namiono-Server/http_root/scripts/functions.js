function LoadDocument(provider, target, action, frame)
{
	var request = $.ajax(
		{
			url: '/',
			method: "GET",
			cache: false,
			beforeSend: function (req) {
				req.setRequestHeader("Request-Type", "async");
				req.setRequestHeader("User-Agent", "Namiono-Client");
				if (action !== '') {
					req.setRequestHeader("Action", action);
				}

				if (provider !== '') {
					req.setRequestHeader("Provider", provider);
				}

				if (target !== '') {
					req.setRequestHeader("Target", target);
				}
			}})
		.done(function (resp) {
			$('#content').fadeOut("100", function () {
				$('#content').html(resp);
				$('#content').fadeIn("200");
			});
		})
		.fail(function (html) {
			$('#content').fadeOut("50", function () {
				$('#content').html(MsgBox(provider, '<p class=\"exclaim\">Die Anfrage wurde beendet! (Fehler: ' + html.statusText + ')</p>'));
				$('#content').fadeIn("1300");
			});
		});
}

function MsgBox(title, message)
{
	var x = "<div id=\"messagebox_err-box\">";
	x += "<div class=\"box-header\">" + title + "</div>\n";
	x += "<div class=\"box-content\">" + message + "</div>\n";
	x += "</div>";

	return x;
}