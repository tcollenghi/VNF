
var wChartIntervalTime = 1;
var wCategoriaComprador = 1;
var wUnidadeMetso = "E";
var wStrTopSQLCommand = '1'
var wStrtimerRfresh;

$(document).ready(function () {
    var vStrChartTitle = "Divergências - INDIRETOS";

    //Ocultar menu lateral
    $('#hide-menu > span > a').trigger("click");

    //Ocultar cursor do mouse
    $('body').mouseover(function () {
        $(this).css({ cursor: 'none' });
    });
   
    GetPrincipalDivergenciasCompraIndiretos(vStrChartTitle);
    
})

function GetPrincipalDivergenciasCompraIndiretos(pStrChartTitle) {

    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: url + "Relatorios/GetDiveCompraIndireto",
        dataType: 'json',
        data: {
                pCategoriaComprador: wCategoriaComprador,
                pStrTopSQLCommand:  wStrTopSQLCommand,
                pUnidadeMetso: wUnidadeMetso
              },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {

            var barOptions_stacked = {
                tooltips: {
                    enabled: false
                },
                hover: {
                    animationDuration: 0
                },
                scales: {
                    xAxes: [{
                        ticks: {
                            beginAtZero: true,
                            fontFamily: "'Open Sans Bold', sans-serif",
                            fontSize: 11,
                        },
                        scaleLabel: {
                            display: false
                        },
                        gridLines: {
                        },
                        stacked: true
                    }],
                    yAxes: [{
                        gridLines: {
                            display: false,
                            color: "#fff",
                            zeroLineColor: "#fff",
                            zeroLineWidth: 0
                        },
                        ticks: {
                            fontFamily: "'Open Sans Bold', sans-serif",
                            fontSize: 11
                        },
                        stacked: true
                    }]
                },
                legend: {
                    display: true,
                    position: 'bottom',

                },

                animation: {
                    onComplete: function () {
                        var chartInstance = this.chart;
                        var ctx = chartInstance.ctx;
                        ctx.font = "11px Open Sans";
                        ctx.fillStyle = "#000000";
                        ctx.textAlign = "left";

                        Chart.helpers.each(this.data.datasets.forEach(function (dataset, i) {
                            var meta = chartInstance.controller.getDatasetMeta(i);
                            Chart.helpers.each(meta.data.forEach(function (bar, index) {
                                data = dataset.data[index];
                                if (data > 0 && data < 10) {
                                    ctx.fillText(data, bar._model.x - 7, bar._model.y - 6);
                                } else if (data > 10) {
                                    ctx.fillText(data, bar._model.x - 25, bar._model.y - 6);
                                }
                            }), this)
                        }), this);
                    }
                },
                pointLabelFontFamily: "Quadon Extra Bold",
                scaleFontFamily: "Quadon Extra Bold",
                responsive: true,
                title: {
                    display: true,
                    text: pStrChartTitle,
                    fontSize: 20,
                    fontStyle: "bold"
                },

            };

            var ctx = document.getElementById("Chart1");
            var myChart = new Chart(ctx, {
                type: 'horizontalBar',
                data: oReturn,
                options: barOptions_stacked,
            });

            wStrtimerRfresh = oReturn.timerRefresh;
            setupRefresh();
          
        }
    });
   
}

function setupRefresh() {
    window.setTimeout("refreshPage();", wStrtimerRfresh);
}
function refreshPage() {
    var vStrChartTitle;

    wChartIntervalTime++;

    if (wChartIntervalTime == 1) {
        vStrChartTitle = "Divergências - INDIRETOS";
        wCategoriaComprador = 1;
        wStrTopSQLCommand = '1'
    } else if(wChartIntervalTime == 2) {
        vStrChartTitle = "Divergências - PRODUTIVOS";
        wCategoriaComprador = 2;
        wStrTopSQLCommand = '1'
    } else if (wChartIntervalTime == 3) {
        vStrChartTitle = "PORTARIA - Unidade EQUIPAMENTOS";
        var wUnidadeMetso = "E";
        wStrTopSQLCommand = '0'
    } else if (wChartIntervalTime == 4) {
        vStrChartTitle = "PORTARIA - Unidade FUNDIÇÃO";
        var wUnidadeMetso = "F";
        wStrTopSQLCommand = '0'
    }

    if (wChartIntervalTime == 4) {
        wChartIntervalTime = 0;
    }
    GetPrincipalDivergenciasCompraIndiretos(vStrChartTitle);
}


