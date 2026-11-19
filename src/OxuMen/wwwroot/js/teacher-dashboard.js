
document.addEventListener("DOMContentLoaded", function () {

    // Nümunə şagird məlumatları
    const students = [
        { name: "Aylin M.", speed: 62, accuracy: 94, comprehension: 7 },
        { name: "Rashad A.", speed: 48, accuracy: 88, comprehension: 6 },
        { name: "Zahra K.", speed: 71, accuracy: 97, comprehension: 8 },
        { name: "Murad H.", speed: 36, accuracy: 82, comprehension: 4 }
    ];

    const dashboard = document.getElementById("teacherAnalytics");

    if (!dashboard) return;

    // Seçilmiş şagirdin nəticələrini göstər
    function selectStudent(index) {
        const student = students[index];

        if (!student) return;

        document.getElementById("teacherSelectedName").textContent =
            student.name;

        document.getElementById("teacherSpeed").textContent =
            student.speed + " söz/dəq";

        document.getElementById("teacherAccuracy").textContent =
            student.accuracy + "%";

        document.getElementById("teacherComprehension").textContent =
            student.comprehension + "/10";

        document.getElementById("teacherSpeedBar").style.width =
            Math.min(100, student.speed) + "%";

        document.getElementById("teacherAccuracyBar").style.width =
            student.accuracy + "%";

        document.getElementById("teacherComprehensionBar").style.width =
            student.comprehension * 10 + "%";

        document.querySelectorAll(".teacher-student-button")
            .forEach(function (button) {

                const selected =
                    Number(button.dataset.student) === index;

                button.classList.toggle("selected", selected);
                button.setAttribute("aria-pressed", String(selected));
            });
    }

    // Sinif üzrə interaktiv qrafik
    function renderChart(metricName) {

        const chart = document.getElementById("teacherClassChart");
        chart.replaceChildren();

        const metrics = {
            speed: {
                max: 100,
                unit: "söz/dəq",
                note: "Oxu sürəti söz/dəqiqə ilə göstərilir."
            },
            accuracy: {
                max: 100,
                unit: "%",
                note: "Düzgünlük faizlə göstərilir."
            },
            comprehension: {
                max: 10,
                unit: "/10",
                note: "Anlama nəticəsi 10 ballıq şkala ilə göstərilir."
            }
        };

        const metric = metrics[metricName];

        if (!metric) return;

        students.forEach(function (student) {

            const value = student[metricName];
            const percentage = (value / metric.max) * 100;

            const column = document.createElement("div");
            column.className = "teacher-chart-column";

            const valueLabel = document.createElement("strong");
            valueLabel.className = "teacher-chart-value";
            valueLabel.textContent = value + metric.unit;

            const bar = document.createElement("div");
            bar.className = "teacher-chart-bar";
            bar.style.height = (percentage * 1.8) + "px";

            const name = document.createElement("span");
            name.className = "teacher-chart-name";
            name.textContent = student.name;

            column.append(valueLabel, bar, name);
            chart.appendChild(column);
        });

        document.getElementById("teacherChartNote").textContent =
            metric.note;

        document.querySelectorAll(".teacher-chart-tab")
            .forEach(function (button) {

                const active = button.dataset.metric === metricName;

                button.classList.toggle("active", active);
                button.setAttribute("aria-pressed", String(active));
            });
    }

    // Şagird seçimi
    document.querySelectorAll(".teacher-student-button")
        .forEach(function (button) {

            button.addEventListener("click", function () {
                selectStudent(Number(button.dataset.student));
            });
        });

    // Qrafik göstəricilərinin seçimi
    document.querySelectorAll(".teacher-chart-tab")
        .forEach(function (button) {

            button.addEventListener("click", function () {
                renderChart(button.dataset.metric);
            });
        });

    // İlkin görünüş
    selectStudent(0);
    renderChart("speed");

});
