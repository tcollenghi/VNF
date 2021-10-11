
$(document).ready(function () {
    pageSetUp();
    $("#btnBloquear").addClass("hide");
    $("#btnLiberar").removeClass("hide");
})

function selectRow(id, tela) {
    $("#row_" + tela).siblings().removeClass("selecionado");
    $("#row_" + tela).toggleClass("selecionado");
    $("#hdfId").val(id);
    $("#hdfTela").val(tela);//Marcio Spinosa - 24/07/2019 
}

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

function KeyEnter(e) {
    var teste = e.keyCode || e.which
    if (teste) {
        LoadData();
    }
}

function LoadData() {

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Acesso/GetDataBloqueado",
        dataType: 'html',
        data: {
            ptxtCodigoPagina: $("#txtCodigoPagina").val(),
            ptxtUser: $("#txtUser").val()//Marcio Spinosa - 24/07/2019 
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (data) {
            $('#definicaoTabela').html(data);
            $("#tabBloqueado").addClass("active");
            $("#tabLiberado").removeClass("active");
            $("#btnBloquear").addClass("hide");
            $("#btnLiberar").removeClass("hide");

            if ($("#demo").hasClass("activate") == true) {
                $("#demo-setting").trigger("click");
            }
        }
    });
}

function GetDataLiberado() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Acesso/GetDataLiberado",
        dataType: 'html',
        data: {
            ptxtCodigoPagina: $("#txtCodigoPagina").val(),
            ptxtUser: $("#txtUser").val()//Marcio Spinosa - 24/07/2019 
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (data) {
            $('#definicaoTabela').html(data);

            $("#btnLiberar").addClass("hide");
            $("#btnBloquear").removeClass("hide");

        }
    });
}

function BloqueiaUsuario() {
    usuario = $("#hdfId").val();
    //Marcio Spinosa - 24/07/2019 
    //txtCodigoPagina = $("#txtCodigoPagina").val();
    //alert($("#hdfTela").val());
    txtCodigoPagina = $("#hdfTela").val().substring(0, 4);
    //Marcio Spinosa - 24/07/2019  - Fim

    if (usuario == "" || txtCodigoPagina == "")
        return;

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Acesso/BloqueiaUsuario",
        dataType: 'json',
        data: {
            txtCodigoPagina: txtCodigoPagina,
            usuario: usuario
        },
        error: function (erro) {

            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.smallBox({
                title: "Alerta",
                content: "Usuário bloqueado",
                color: "#739E73",
                icon: "fa fa-check-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            GetDataLiberado();
        }
    });
}

function LiberaUsuario() {

    usuario = $("#hdfId").val();
    txtCodigoPagina = $("#hdfTela").val().substring(0, 4);


    if (usuario == "" || txtCodigoPagina == "")
        return;

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Acesso/LiberaUsuario",
        dataType: 'json',
        data: {
            txtCodigoPagina: txtCodigoPagina,
            usuario: usuario
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.smallBox({
                title: "Alerta",
                content: "Usuário liberado",
                color: "#739E73",
                icon: "fa fa-check-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            LoadData();
        }
    });
}