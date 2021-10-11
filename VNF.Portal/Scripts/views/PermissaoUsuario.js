function PermissaoUsuario(pAcesso) {

    var acesso = pAcesso;
    var permissao = " ";
    if (pAcesso.split('|').length > 1) {
        acesso = pAcesso.split('|')[0];
        permissao = " (" + pAcesso.split('|')[1] + ") ";
    }

    if (acesso == "false") {
        var msg = "Seu usuário não tem permissão" + permissao + "para acessar as informações desta tela.";

        $("#content").html(msg);

        $.SmartMessageBox({
            title: "Alerta de Acesso",
            content: msg,
            buttons: '[Ok]'
        }, function (ButtonPressed) {
            if (ButtonPressed === "Ok") {
                window.history.back();
            }
        });
    }
}