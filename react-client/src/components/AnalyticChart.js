import { useState, useEffect, useCallback } from 'react';
import { Bar } from 'react-chartjs-2';
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, ArcElement, Title, Tooltip, Legend } from 'chart.js';
import { Eye, EyeOff, DatabaseBackup, DatabaseZap } from 'lucide-react';

ChartJS.register(CategoryScale, LinearScale, BarElement, ArcElement, Title, Tooltip, Legend);

const BASE_URL = 'http://localhost:5123';

const AnalyticChart = () => {
    const [data, setData] = useState([]);
    const [metric, setMetric] = useState('agpt');
    const [showTable, setShowTable] = useState(false);
    const [showChart, setShowChart] = useState(true);
    const [loading, setLoading] = useState(false);
    const [isDbEmpty, setIsDbEmpty] = useState(true);

    const labelOptions = [
        { value: "agpt", label: "Average Grades Per Teacher" },
        { value: "esbd", label: "Enrollment Summary by Department" },
        { value: "asst", label: "Attendance Summary by Student & Term" },
        { value: "ccbt", label: "Class Count by Teacher (Grouped)" },
        { value: "ecpc", label: "Enrollment Count Per Class" }
    ];

    const fetchData = useCallback(async (metricName = metric) => {
        try {
        setLoading(true);
        const res = await fetch(`${BASE_URL}/api/reports/analytics?metric=${metricName}`);
        if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);
        const jsonData = await res.json();
        setData(jsonData);
        setIsDbEmpty(jsonData.length === 0);
        } catch (err) {
        console.error('Failed to load data:', err);
        setData([]);
        setIsDbEmpty(true);
        } finally {
        setLoading(false);
        }
    }, [metric]);

    useEffect(() => {
    fetchData();
    }, [metric, fetchData]);

    // Add debug logs
    useEffect(() => {
    console.log("Fetched data:", data);
    }, [data]);

    useEffect(() => {
        setShowTable(true);
        setShowChart(!isTableOnlyMetric(metric));
    }, [metric]);

    const labelKey = data.length ? Object.keys(data[0])[0] : 'label';
    const valueKey = data.length
    ? Object.keys(data[0]).at(-1)   // always use the last column
    : 'metricValue';

    // Get the display label for the current metric
    const getMetricDisplayLabel = () => {
        const option = labelOptions.find(option => option.value === metric);
        return option ? option.label : metric;
    };

    const maxBars = 10;

    // Limit data for chart
    const limitedData = data.slice(0, maxBars);

    const chartData = {
        labels: limitedData.map(item => item[labelKey]),
        datasets: [
            {
                label: getMetricDisplayLabel(),
                data: limitedData.map(item => Number(item[valueKey] ?? 0)), // ensure numeric
                backgroundColor: '#4bc06e',
            },
        ],
    };


    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
        legend: { 
            position: 'top',
            labels: {
                    generateLabels: (chart) => {
                        return [{
                            text: getMetricDisplayLabel(),
                            fillStyle: '#4bc06e',
                            strokeStyle: '#5ec17c',
                            lineWidth: 1,
                            hidden: false,
                            index: 0
                        }];
                    }
                }
         },
            title: { 
                display: true, 
                text: getMetricDisplayLabel(),
                font: {
                    size: 16,
                    weight: 'bold'
                },
                padding: {
                    top: 10,
                    bottom: 30
                }
            }
        }
    };

    const handleEtlClick = async () => {
        const endpoint = isDbEmpty
        ? `${BASE_URL}/api/etl/run`
        : `${BASE_URL}/api/etl/purge`;

        try {
        const response = await fetch(endpoint, { method: 'GET' });
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        fetchData();
        } catch (err) {
        console.error('ETL action failed:', err);
        alert('ETL action failed. Please check if the backend is running.');
        }
    };

    const isTableOnlyMetric = (metricName) => [].includes(metricName);

    return (
        <div className="analytic-chart-container">
        <div className="analytic-chart-header">
            <div className="analytic-chart-actions">
            <select value={metric} onChange={(e) => setMetric(e.target.value)}>
                {labelOptions.map((label) => (
                <option key={label.value} value={label.value}>
                    {label.label}
                </option>
                ))}
            </select>
            <div className="buttonGroup">
                <button onClick={() => setShowChart(!showChart)}>
                {showChart ? <EyeOff /> : <Eye />} Show Chart
                </button>
                <button onClick={() => setShowTable(!showTable)}>
                {showTable ? <EyeOff /> : <Eye />} Show Table
                </button>
                <button onClick={handleEtlClick}>
                {isDbEmpty ? <DatabaseBackup /> : <DatabaseZap />} 
                {isDbEmpty ? "Run ETL" : "Purge Data"}
                </button>
            </div>
            </div>
        </div>
        <div className="analytic-chart-content">
            {loading ? (
            <p>Loading data...</p>
            ) : (
            <>
                {showChart && !isTableOnlyMetric(metric) && (
                <div className="chart-wrapper" style={{ height: '400px' }}>
                    <Bar data={chartData} options={chartOptions} />
                </div>
                )}
                {showTable && (
                <table className="data-table">
                    <thead>
                        <tr>
                            {data.length > 0 &&
                            Object.keys(data[0]).map((col) => (
                                <th key={col}>{col}</th>
                            ))}
                        </tr>
                    </thead>
                    <tbody>
                    {data.map((row, rowIndex) => (
                        <tr key={rowIndex}>
                        {Object.keys(row).map((col, colIndex) => (
                            <td key={colIndex}>{row[col]}</td>
                        ))}
                        </tr>
                    ))}
                    </tbody>
                </table>
                )}
            </>
            )}
        </div>
        </div>
    );
};

export default AnalyticChart;