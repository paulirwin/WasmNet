;; invoke: logIt
;; expect: (void:)
;; TODO: assert console.log call and argument

(module
  (import "console" "log" (func $log (param i32)))
  (func (export "logIt")
    i32.const 13
    call $log))

