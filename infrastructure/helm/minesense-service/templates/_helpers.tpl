{{/* Name of every object of the release: the service name. */}}
{{- define "minesense-service.name" -}}
{{- required "name is required (kebab-case service name)" .Values.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "minesense-service.labels" -}}
app.kubernetes.io/name: {{ include "minesense-service.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Values.image.tag | quote }}
app.kubernetes.io/part-of: minesense-safety
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version }}
{{- end -}}

{{- define "minesense-service.selectorLabels" -}}
app.kubernetes.io/name: {{ include "minesense-service.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

{{- define "minesense-service.image" -}}
{{- $repository := required "image.repository is required" .Values.image.repository -}}
{{- printf "%s/%s:%s" .Values.image.registry $repository (toString .Values.image.tag) -}}
{{- end -}}
