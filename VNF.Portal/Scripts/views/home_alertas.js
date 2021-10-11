

// With Callback
$("#btnReprovar").click(function (e) {
    $.SmartMessageBox({
        title: "Negociação de data",
        content: "Informe o motivo da negociação de data não ser autorizada",
        buttons: '[Cancelar][Confirmar]',
        input: "text",
        placeholder: "Escreva aqui"
    }, function (ButtonPressed, Value) {
        if (ButtonPressed === "Confirmar") {
            $.smallBox({
                title: "Solicitação negada",
                content: "<i class='fa fa-clock-o'></i> <i>A alteração de data de entrega não foi autorizada</i>",
                color: "#C46A69",
                iconSmall: "fa fa-times fa-2x fadeInRight animated",
                timeout: 4000
            });
        }

    });
    e.preventDefault();
})


// With Callback
$("#btnAprovar").click(function (e) {
    $.smallBox({
        title: "Solicitação aprovada",
        content: "<i class='fa fa-clock-o'></i> <i>A alteração de data de entrega foi autorizada</i>",
        color: "#659265",
        iconSmall: "fa fa-check fa-2x fadeInRight animated",
        timeout: 4000
    });
    e.preventDefault();
})
