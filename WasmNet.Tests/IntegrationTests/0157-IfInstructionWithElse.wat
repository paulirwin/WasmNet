;; invoke: ifelse (i32:0)
;; expect: (i32:42)
;; invoke: ifelse (i32:1)
;; expect: (i32:100)

(module
    (func (export "ifelse") (param $x i32) (result i32)
        (if (result i32) (i32.eq (local.get $x) (i32.const 0))
            (then (i32.const 42))
            (else (i32.const 100))
        )
    )
)