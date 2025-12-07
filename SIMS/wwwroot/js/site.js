// SIMS - Student Information Management System
// JavaScript functionality

$(document).ready(function () {
    // Initialize tooltips
    $('[title]').tooltip();
    
    // Active navigation highlighting
    highlightActiveNavigation();
    
    // Auto dismiss alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut();
    }, 5000);
    
    // Form validation enhancement
    enhanceFormValidation();
    
    // Table enhancements
    enhanceDataTables();
    
    // Smooth scrolling for anchor links
    $('a[href^="#"]').on('click', function(e) {
        var href = this.getAttribute('href');
        if (href === '#' || href === '') {
            return; // Skip smooth scrolling for empty or just "#" hrefs
        }
        e.preventDefault();
        var target = $(href);
        if (target.length) {
            $('html, body').stop().animate({
                scrollTop: target.offset().top - 100
            }, 300);
        }
    });
});

// Highlight active navigation item
function highlightActiveNavigation() {
    var currentPath = window.location.pathname.toLowerCase();
    $('.nav-link').each(function() {
        var linkPath = $(this).attr('href');
        if (linkPath && currentPath.includes(linkPath.toLowerCase())) {
            $(this).addClass('active');
            $(this).closest('.nav-item').addClass('active');
        }
    });
}

// Enhance form validation
function enhanceFormValidation() {
    $('form').on('submit', function() {
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        
        // Show loading state
        submitBtn.prop('disabled', true);
        submitBtn.html('<span class="spinner"></span> Processing...');
        
        // Reset after 5 seconds if form doesn't submit
        setTimeout(function() {
            submitBtn.prop('disabled', false);
            submitBtn.html(originalText);
        }, 5000);
    });
    
    // Real-time validation feedback (skip for forms with no-auto-validate class)
    $('.form-control').on('blur', function() {
        // Skip validation for forms marked as no-auto-validate
        if ($(this).closest('form').hasClass('no-auto-validate')) {
            return;
        }
        
        // Skip validation for readonly fields
        if ($(this).prop('readonly') || $(this).prop('disabled')) {
            return;
        }
        
        var input = $(this);
        var isValid = this.checkValidity();
        
        input.removeClass('is-valid is-invalid');
        
        if (input.val().length > 0) {
            if (isValid) {
                input.addClass('is-valid');
            } else {
                input.addClass('is-invalid');
            }
        }
    });
}

// Enhance data tables
function enhanceDataTables() {
    // Add search functionality to tables
    $('.table').each(function() {
        if ($(this).find('tbody tr').length > 5) {
            var tableId = 'table_' + Math.random().toString(36).substr(2, 9);
            $(this).attr('id', tableId);
            
        }
    });
}

// Table filtering function
function filterTable(tableId, searchValue) {
    var table = document.getElementById(tableId);
    var tbody = table.getElementsByTagName('tbody')[0];
    var rows = tbody.getElementsByTagName('tr');
    
    searchValue = searchValue.toLowerCase();
    
    for (var i = 0; i < rows.length; i++) {
        var row = rows[i];
        var cells = row.getElementsByTagName('td');
        var found = false;
        
        for (var j = 0; j < cells.length; j++) {
            var cellText = cells[j].textContent || cells[j].innerText;
            if (cellText.toLowerCase().indexOf(searchValue) > -1) {
                found = true;
                break;
            }
        }
        
        row.style.display = found ? '' : 'none';
    }
}

// Confirmation dialogs
function confirmDelete(message) {
    return confirm(message || 'Are you sure you want to delete this item?');
}

// Toast notifications
function showToast(message, type = 'info') {
    var toastHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    // Create toast container if it doesn't exist
    if (!$('#toast-container').length) {
        $('body').append('<div id="toast-container" class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
    }
    
    var $toast = $(toastHtml);
    $('#toast-container').append($toast);
    
    var toast = new bootstrap.Toast($toast[0]);
    toast.show();
    
    // Remove from DOM after hide
    $toast.on('hidden.bs.toast', function() {
        $(this).remove();
    });
}

// Sidebar toggle for mobile
function toggleSidebar() {
    $('.sidebar').toggleClass('show');
}

// Auto-refresh dashboard stats (every 30 seconds)
function autoRefreshStats() {
    if (window.location.pathname.includes('/Dashboard')) {
        setInterval(function() {
            // Only refresh if page is visible
            if (!document.hidden) {
                $('.stat-value').each(function() {
                    $(this).fadeOut(200).fadeIn(200);
                });
            }
        }, 30000);
    }
}

// Initialize auto-refresh
autoRefreshStats();

// Handle responsive sidebar
$(window).resize(function() {
    if ($(window).width() > 768) {
        $('.sidebar').removeClass('show');
    }
});

// Course enrollment with animation
function enrollInCourse(courseId) {
    var btn = $(`button[onclick*="${courseId}"]`);
    var originalHtml = btn.html();
    
    btn.prop('disabled', true);
    btn.html('<span class="spinner"></span> Enrolling...');
    
    // Simulate API call (replace with actual AJAX call)
    setTimeout(function() {
        btn.removeClass('btn-success').addClass('btn-link');
        btn.html('<i class="fas fa-check"></i> Enrolled');
        showToast('Successfully enrolled in course!', 'success');
    }, 1500);
}

// Profile image upload preview
function previewImage(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function(e) {
            $('#avatar-preview').attr('src', e.target.result);
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Form auto-save (for profile editing)
let autoSaveTimeout;
function autoSaveForm() {
    clearTimeout(autoSaveTimeout);
    autoSaveTimeout = setTimeout(function() {
        showToast('Changes auto-saved', 'info');
    }, 2000);
}

// Initialize form auto-save
$('.auto-save').on('input', autoSaveForm);

// Animation helpers
function animateCount(element, start, end, duration) {
    var range = end - start;
    var current = start;
    var increment = end > start ? 1 : -1;
    var stepTime = Math.abs(Math.floor(duration / range));
    
    var timer = setInterval(function() {
        current += increment;
        $(element).text(current);
        if (current == end) {
            clearInterval(timer);
        }
    }, stepTime);
}

// Animate stats on dashboard load
$(window).on('load', function() {
    $('.stat-value').each(function() {
        var finalValue = parseInt($(this).text());
        $(this).text('0');
        animateCount(this, 0, finalValue, 1000);
    });
});

// Enhanced loading states
function showLoadingOverlay(message = 'Loading...') {
    var overlay = `
        <div id="loading-overlay" class="position-fixed w-100 h-100" style="top:0;left:0;background:rgba(0,0,0,0.5);z-index:9999;">
            <div class="d-flex align-items-center justify-content-center h-100">
                <div class="text-center text-white">
                    <div class="spinner-border text-primary mb-3" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p>${message}</p>
                </div>
            </div>
        </div>
    `;
    $('body').append(overlay);
}

function hideLoadingOverlay() {
    $('#loading-overlay').remove();
}

// Print functionality
function printPage() {
    window.print();
}

// Export table data
function exportTableToCSV(tableId, filename) {
    var csv = [];
    var table = document.getElementById(tableId);
    var rows = table.querySelectorAll('tr');
    
    for (var i = 0; i < rows.length; i++) {
        var row = [], cols = rows[i].querySelectorAll('td, th');
        
        for (var j = 0; j < cols.length - 1; j++) { // Skip last column (actions)
            row.push('"' + cols[j].innerText.replace(/"/g, '""') + '"');
        }
        
        csv.push(row.join(','));
    }
    
    downloadCSV(csv.join('\n'), filename);
}

function downloadCSV(csv, filename) {
    var csvFile = new Blob([csv], {type: 'text/csv'});
    var downloadLink = document.createElement('a');
    downloadLink.download = filename;
    downloadLink.href = window.URL.createObjectURL(csvFile);
    downloadLink.style.display = 'none';
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}

// Console welcome message
console.log('%cWelcome to SIMS! 🎓', 'color: #ff6b35; font-size: 24px; font-weight: bold;');
console.log('%cStudent Information Management System', 'color: #666; font-size: 14px;');
console.log('%cBuilt with ASP.NET Core & modern web technologies', 'color: #999; font-size: 12px;');
