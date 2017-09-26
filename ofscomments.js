var script = document.createElement('script');
script.src = 'https://ajax.googleapis.com/ajax/libs/jquery/3.2.1/jquery.min.js';
script.type = 'text/javascript';
document.getElementsByTagName('head')[0].appendChild(script);

var azureFuncUrl = null;

function OnClick() {
    if (!azureFuncUrl) {
        alert('URL not set! Edit ofscomments.js');
    } else {
        $.ajax(azureFuncUrl, {
            type: "POST",
            data: JSON.stringify({ name: "open fsharp conf" }),
            success: function (data, textStatus, jqXHR) { alert('Sent message to F# Azure Function!\nResponse:\n' + data); },
            error: function (jqXHR, textStatus, errorThrown) { alert('Failed to send message!\n' + textStatus); }
        });
    }
}