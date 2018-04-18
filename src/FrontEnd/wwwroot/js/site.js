var app = angular.module('TeamAssemblerApp', ['ui.bootstrap']);
app.run(function () { });

app.controller('TeamAssemblerController', ['$rootScope', '$scope', '$http', '$timeout', function ($rootScope, $scope, $http, $timeout) {

    $scope.selectedMembers = {};

    $scope.init = function () {
        $scope.getMembers();
        $scope.refresh();
    }

    $scope.refresh = function () {

        $http.get('api/Teams?c=' + new Date().getTime())
            .then(function (response, status) {
                $scope.teams = response.data;
            }, function (response, status) {
                $scope.teams = undefined;
            });
    };

    $scope.getMembers = function () {
        $http.get('api/Members')
            .then(function (response, status) {
                $scope.members = response.data;
            }, function (response, status) {
                $scope.members = undefined;
            });
    };

    $scope.assemble = function () {

        var members = [];
        angular.forEach($scope.selectedMembers, function (selected, member) {
            if (selected) {
                members.push(member);
            }
        });
        var request = JSON.stringify({
            name: $scope.teamName,
            members: members
        });
        $http.put('api/Teams/' + $scope.teamName, JSON.stringify(members), {
                headers: { 'Content-Type': 'application/json' }
            })
            .then(function (response, status) {
                $scope.refresh();
                $scope.teamName = '';
                $scope.selectedMembers = {};
            });
    };

    $scope.remove = function (name) {
        $http.delete('api/Teams/' + name)
            .then(function (response, status) {
                $scope.refresh();
            })
    };

    $scope.renderIntelligence = function (value) {
        switch (value) {
            case 1: return "Slow/Impaired";
            case 2: return "Normal";
            case 3: return "Learned";
            case 4: return "Gifted";
            case 5: return "Genius";
            case 6: return "Super-Genius";
            case 7: return "Omniscient";
            default: return "Unknown";
        }
    };

    $scope.renderStrength = function (value) {
        switch (value) {
            case 1: return "Weak";
            case 2: return "Normal";
            case 3: return "Peak human";
            case 4: return "Superhuman (800lbs-25 ton range)";
            case 5: return "Superhuman (25-75 ton range)";
            case 6: return "Superhuman (75-100 ton range)";
            case 7: return "Incalculable (100+ ton range)";
            default: return "Unknown";
        }
    };

    $scope.renderSpeed = function (value) {
        switch (value) {
            case 1: return "Below normal";
            case 2: return "Normal";
            case 3: return "Superhuman (peak range: 700 MPH)";
            case 4: return "Speed of sound (Mach-1)";
            case 5: return "Supersonic (Mach-2 through Orbital Velocity)";
            case 6: return "Speed of light (186,000 miles per second)";
            case 7: return "Warp speed (transcending light speed)";
            default: return "Unknown";
        }
    };

    $scope.renderDurability = function (value) {
        switch (value) {
            case 1: return "Weak";
            case 2: return "Normal";
            case 3: return "Enhanced";
            case 4: return "Regenerative";
            case 5: return "Bulletproof";
            case 6: return "Superhuman";
            case 7: return "Virtually indestructible";
            default: return "Unknown";
        }
    };

    $scope.renderEnergyProjection = function (value) {
        switch (value) {
            case 1: return "None";
            case 2: return "Ability to discharge energy on contact";
            case 3: return "Short range, short duration, single energy type";
            case 4: return "Medium range, medium duration, single energy type";
            case 5: return "Long range, long duration, single energy type";
            case 6: return "Able to discharge multiple forms of energy";
            case 7: return "Virtually unlimited command of all forms of energy";
            default: return "Unknown";
        }
    };

    $scope.renderFightingSkills = function (value) {
        switch (value) {
            case 1: return "Poor";
            case 2: return "Normal";
            case 3: return "Some training";
            case 4: return "Experienced fighter";
            case 5: return "Master of a single form of combat";
            case 6: return "Master of several forms of combat";
            case 7: return "Master of all forms of combat";
            default: return "Unknown";
        }
    };
}]);