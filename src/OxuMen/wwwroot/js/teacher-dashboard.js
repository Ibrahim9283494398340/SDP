// Interactive part of the teacher dashboard (Home/Index).
// The student list comes from the page (#teacherDashboardData): the latest
// result of each student in the teacher's own classes, or sample students
// when there are no results yet. Values can be null when a result has no
// value for that measure; they are shown as "—".
document.addEventListener("DOMContentLoaded", function () {

    const dashboard = document.getElementById("teacherAnalytics");
    const dataElement = document.getElementById("teacherDashboardData");

    if (!dashboard || !dataElement) return;

    let students = [];

    try {
        students = JSON.parse(dataElement.textContent) || [];
    } catch (error) {
        students = [];
    }

    if (students.length === 0) return;

    function hasValue(value) {
        return value !== null && value !== undefined;
    }

    function percent(value, max) {
        if (!hasValue(value) || max <= 0) return 0;
        return Math.max(0, Math.min(100, (value / max) * 100));
    }

    // The speed scale grows when a student reads faster than 100 words/min.
    const speedMax = Math.max(
        100,
        ...students.map(function (s) { return hasValue(s.speed) ? s.speed : 0; })
    );

    const metrics = {
        speed: {
            max: speedMax,
            unit: " söz/dəq",
            note: "Oxu sürəti söz/dəqiqə ilə göstərilir."
        },
        accuracy: {
            max: 100,
            unit: "%",
            note: "Düzgünlük faizlə göstərilir."
        },
        comprehension: {
            max: 100,
            unit: "%",
            note: "Anlama düzgün cavabların faizi ilə göstərilir."
        }
    };

    function format(value, unit) {
        return hasValue(value) ? value + unit : "—";
    }

    // Seçilmiş şagirdin nəticələrini göstər
    function selectStudent(index) {
        const student = students[index];

        if (!student) return;

        document.getElementById("teacherSelectedName").textContent =
            student.name;

        document.getElementById("teacherSpeed").textContent =
            format(student.speed, metrics.speed.unit);

        document.getElementById("teacherAccuracy").textContent =
            format(student.accuracy, metrics.accuracy.unit);

        document.getElementById("teacherComprehension").textContent =
            format(student.comprehension, metrics.comprehension.unit);

        document.getElementById("teacherSpeedBar").style.width =
            percent(student.speed, metrics.speed.max) + "%";

        document.getElementById("teacherAccuracyBar").style.width =
            percent(student.accuracy, metrics.accuracy.max) + "%";

        document.getElementById("teacherComprehensionBar").style.width =
            percent(student.comprehension, metrics.comprehension.max) + "%";

        const link = document.getElementById("teacherResultLink");

        if (link) {
            if (student.url) {
                link.href = student.url;
                link.hidden = false;
            } else {
                link.hidden = true;
            }
        }

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
        const metric = metrics[metricName];

        if (!chart || !metric) return;

        chart.replaceChildren();

        students.forEach(function (student) {

            const value = student[metricName];

            const column = document.createElement("div");
            column.className = "teacher-chart-column";

            const valueLabel = document.createElement("strong");
            valueLabel.className = "teacher-chart-value";
            valueLabel.textContent = format(value, metric.unit);

            const bar = document.createElement("div");
            bar.className = "teacher-chart-bar";
            bar.style.height = (percent(value, metric.max) * 1.8) + "px";

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
